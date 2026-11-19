using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Tests.Domain;

public class ServiceRequestTests
{
  private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

  private static ServiceRequest NewRequest() => ServiceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "Instalación", Now);

  [Fact]
  public void Completed_never_returns_to_a_previous_status()
  {
    var request = NewRequest();
    request.StartProcessing(Now);
    request.Complete(Now);

    Assert.Throws<InvalidStatusTransitionException>(() => request.StartProcessing(Now));
    Assert.Throws<InvalidStatusTransitionException>(() => request.RegisterTransientFailure("x", 3, Now));
    Assert.Throws<InvalidStatusTransitionException>(() => request.Complete(Now));
    Assert.Throws<InvalidStatusTransitionException>(() => request.ReleaseExpiredLease(3, Now.AddHours(1), TimeSpan.FromMinutes(5)));
    Assert.Equal(ServiceRequestStatus.Completed, request.Status);
  }

  [Fact]
  public void Transient_failure_returns_to_pending_until_max_attempts_then_fails()
  {
    var request = NewRequest();

    request.StartProcessing(Now);
    request.RegisterTransientFailure("caído", maxAttempts: 2, Now);
    Assert.Equal(ServiceRequestStatus.Pending, request.Status);
    Assert.Equal(1, request.AttemptCount);

    request.StartProcessing(Now);
    request.RegisterTransientFailure("caído", maxAttempts: 2, Now);
    Assert.Equal(ServiceRequestStatus.Failed, request.Status);
    Assert.Equal(2, request.AttemptCount);
    Assert.Equal("caído", request.LastError);
    Assert.Throws<InvalidStatusTransitionException>(() => request.StartProcessing(Now));
  }

  [Fact]
  public void Only_pending_can_start_processing()
  {
    var request = NewRequest();
    request.StartProcessing(Now);

    Assert.Throws<InvalidStatusTransitionException>(() => request.StartProcessing(Now));
  }

  [Fact]
  public void Expired_lease_is_released_but_an_active_one_is_not()
  {
    var lease = TimeSpan.FromMinutes(5);
    var request = NewRequest();
    request.StartProcessing(Now);

    Assert.Throws<InvalidStatusTransitionException>(() => request.ReleaseExpiredLease(3, Now.AddMinutes(1), lease));

    request.ReleaseExpiredLease(3, Now.Add(lease), lease);
    Assert.Equal(ServiceRequestStatus.Pending, request.Status);
  }

  [Theory]
  [InlineData("00000000-0000-0000-0000-000000000000", "3b0e6f0a-8f6b-4c1e-9d55-1d3c2f7e9a01", "ok")]
  [InlineData("6f1c2c8e-1d1f-4a53-9a3f-0d1d5f3a2b11", "00000000-0000-0000-0000-000000000000", "ok")]
  [InlineData("6f1c2c8e-1d1f-4a53-9a3f-0d1d5f3a2b11", "3b0e6f0a-8f6b-4c1e-9d55-1d3c2f7e9a01", "   ")]
  public void Create_rejects_invalid_input(string externalReference, string customerId, string description)
  {
    Assert.Throws<DomainValidationException>(() =>
      ServiceRequest.Create(Guid.Parse(externalReference), Guid.Parse(customerId), description, Now));
  }

  [Fact]
  public void Create_rejects_description_longer_than_max()
  {
    var description = new string('a', ServiceRequest.DescriptionMaxLength + 1);

    Assert.Throws<DomainValidationException>(() => ServiceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), description, Now));
  }
}
