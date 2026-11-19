namespace Domain.Entities;

public enum ServiceRequestStatus { Pending = 1, Processing = 2, Completed = 3, Failed = 4 }
public class ServiceRequest
{
  public Guid Id { get; private set; }
  public Guid ExternarReference { get; private set; }
  public Guid CustomerId { get; private set; }
  public int ServiceCode { get; private set; }
  public string Description { get; private set; }
  public ServiceRequestStatus Status { get; private set; }

  private ServiceRequest()
  {
    Id = Guid.NewGuid();
    Description = String.Empty;
  }

  public static ServiceRequest Create(Guid externalReference, Guid customerId, int serviceCode, string description)
  {
    return new ServiceRequest()
    {
      ExternarReference = externalReference,
      CustomerId = customerId,
      ServiceCode = serviceCode,
      Description = description
    };
  }
}