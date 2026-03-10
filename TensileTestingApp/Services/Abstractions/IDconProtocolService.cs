namespace TensileTestingApp.Services.Abstractions
{
    public interface IDconProtocolService
    {
        void SetAddress(int address);
        string GetReadCommand();
    }
}
