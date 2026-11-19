using Application.Processing;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Tests.Fakes;

namespace Tests.Application;

public class ProcessPendingServiceRequestsTests
{
  private static readonly ProcessingOptions Options = new() { BatchSize = 10, MaxAttempts = 3, LeaseTimeout = TimeSpan.FromMinutes(5) };

  private readonly InMemoryServiceRequestStore _store = new();
  private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));

  private ProcessPendingServiceRequests CreateUseCase(FakeProviderClient provider) =>
    new(_store, _store, provider, Options, _time, NullLogger<ProcessPendingServiceRequests>.Instance);

  private ServiceRequest SeedPending()
  {
    var request = ServiceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "Instalación", _time.GetUtcNow());
    _store.Seed(request);
    return request;
  }

  [Fact]
  public async Task Transient_failure_is_retried_on_next_run_and_completes_with_a_single_effect()
  {
    var request = SeedPending();
    var provider = new FakeProviderClient(ScriptedResponse.TransientFailure, ScriptedResponse.Success);
    var useCase = CreateUseCase(provider);

    await useCase.ExecuteAsync();
    Assert.Equal(ServiceRequestStatus.Pending, request.Status);
    Assert.Equal(1, request.AttemptCount);

    await useCase.ExecuteAsync();
    Assert.Equal(ServiceRequestStatus.Completed, request.Status);
    Assert.Equal(2, request.AttemptCount);
    Assert.Equal(1, provider.EffectsFor(request.ExternalReference));
  }

  [Fact]
  public async Task Request_fails_after_max_attempts_and_is_not_sent_again()
  {
    var request = SeedPending();
    var provider = new FakeProviderClient(Enumerable.Repeat(ScriptedResponse.TransientFailure, 10).ToArray());
    var useCase = CreateUseCase(provider);

    for (var i = 0; i < 5; i++)
      await useCase.ExecuteAsync();

    Assert.Equal(ServiceRequestStatus.Failed, request.Status);
    Assert.Equal(Options.MaxAttempts, request.AttemptCount);
    Assert.Equal(Options.MaxAttempts, provider.Calls.Count);
  }

  [Fact]
  public async Task Timeout_after_provider_applied_the_effect_does_not_duplicate_it_on_retry()
  {
    var request = SeedPending();
    var provider = new FakeProviderClient(ScriptedResponse.ApplyThenTimeout);
    var useCase = CreateUseCase(provider);

    await useCase.ExecuteAsync();
    Assert.Equal(ServiceRequestStatus.Pending, request.Status);

    await useCase.ExecuteAsync();
    Assert.Equal(ServiceRequestStatus.Completed, request.Status);
    Assert.Equal(2, provider.Calls.Count);
    Assert.Equal(1, provider.EffectsFor(request.ExternalReference));
  }

  [Fact]
  public async Task Request_abandoned_in_processing_is_recovered_after_lease_without_duplicating_the_effect()
  {
    // Un worker anterior la tomó, el proveedor aplicó el efecto y el proceso murió antes de guardar Completed.
    var request = SeedPending();
    request.StartProcessing(_time.GetUtcNow());
    var provider = new FakeProviderClient();
    provider.MarkAlreadyProcessed(request.ExternalReference);
    var useCase = CreateUseCase(provider);

    await useCase.ExecuteAsync();
    Assert.Equal(ServiceRequestStatus.Processing, request.Status);
    Assert.Empty(provider.Calls);

    _time.Advance(Options.LeaseTimeout);
    var summary = await useCase.ExecuteAsync();

    Assert.Equal(1, summary.Released);
    Assert.Equal(ServiceRequestStatus.Completed, request.Status);
    Assert.Equal(1, provider.EffectsFor(request.ExternalReference));
  }

  [Fact]
  public async Task Request_claimed_by_another_worker_is_skipped_without_calling_the_provider()
  {
    SeedPending();
    var provider = new FakeProviderClient();
    _store.BeforeSave = _ => throw new ConcurrencyConflictException("otra instancia la tomó");

    var summary = await CreateUseCase(provider).ExecuteAsync();

    Assert.Equal(1, summary.Skipped);
    Assert.Empty(provider.Calls);
  }

  [Fact]
  public async Task Completed_requests_are_never_reprocessed()
  {
    var request = SeedPending();
    var provider = new FakeProviderClient();
    var useCase = CreateUseCase(provider);
    await useCase.ExecuteAsync();

    _time.Advance(TimeSpan.FromHours(1));
    await useCase.ExecuteAsync();

    Assert.Equal(ServiceRequestStatus.Completed, request.Status);
    Assert.Single(provider.Calls);
  }
}
