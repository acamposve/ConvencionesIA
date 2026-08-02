namespace DocumentIngestion.Application;

public partial class Program
{
    public static void Main(string[] args)
    {
        DocumentIngestionApiHost.BuildApp(args).Run();
    }
}
