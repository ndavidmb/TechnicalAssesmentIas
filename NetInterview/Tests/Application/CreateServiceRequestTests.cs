using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Time.Testing;
using Tests.Fakes;

namespace Tests.Application;

public class CreateServiceRequestTests
{
  private readonly InMemoryServiceRequestStore _store = new();
  private readonly ServiceRequestService _service;

  public CreateServiceRequestTests()
  {
    _service = new ServiceRequestService(_store, _store, new FakeTimeProvider());
  }

  [Fact]
  public async Task Retrying_with_same_external_reference_and_payload_creates_a_single_record()
  {
    var externalReference = Guid.NewGuid();
    var customerId = Guid.NewGuid();

    var first = await _service.CreateServiceRequestAsync(externalReference, customerId, "Instalación");
    var retry = await _service.CreateServiceRequestAsync(externalReference, customerId, "Instalación");

    Assert.True(first.Created);
    Assert.False(retry.Created);
    Assert.Equal(first.ServiceRequest.Id, retry.ServiceRequest.Id);
    Assert.Single(_store.Stored);
  }

  [Fact]
  public async Task Same_external_reference_with_different_payload_is_a_conflict()
  {
    var externalReference = Guid.NewGuid();
    var customerId = Guid.NewGuid();
    await _service.CreateServiceRequestAsync(externalReference, customerId, "Instalación");

    await Assert.ThrowsAsync<ExternalReferenceConflictException>(() =>
      _service.CreateServiceRequestAsync(externalReference, customerId, "Otra cosa"));
    Assert.Single(_store.Stored);
  }

  [Fact]
  public async Task Concurrent_insert_rejected_by_unique_index_returns_the_existing_request()
  {
    var externalReference = Guid.NewGuid();
    var customerId = Guid.NewGuid();
    var winner = ServiceRequest.Create(externalReference, customerId, "Instalación", DateTimeOffset.UtcNow);

    // Otra petición insertó el mismo externalReference entre nuestra consulta y nuestro guardado.
    _store.BeforeSave = _ =>
    {
      _store.BeforeSave = null;
      _store.Seed(winner);
      throw new UniqueConstraintViolationException("duplicado");
    };

    var result = await _service.CreateServiceRequestAsync(externalReference, customerId, "Instalación");

    Assert.False(result.Created);
    Assert.Equal(winner.Id, result.ServiceRequest.Id);
    Assert.Single(_store.Stored);
  }
}
