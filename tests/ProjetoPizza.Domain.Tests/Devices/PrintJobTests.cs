using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Tests.Devices;

public sealed class PrintJobTests
{
    [Fact]
    public void Failed_job_can_be_retried_and_completed()
    {
        var job = new PrintJob(
            PrintJobId.New(), RestaurantUnitId.New(), DeviceId.New(),
            PrintDocumentType.TestPage, "Teste de impressão");

        job.Start();
        job.Fail("Impressora offline");
        job.Start();
        job.Complete();

        Assert.Equal(PrintJobStatus.Completed, job.Status);
        Assert.Equal(2, job.Attempts);
        Assert.Null(job.LastError);
        Assert.NotNull(job.CompletedAt);
    }
}
