using TensileTestingApp.Models;

namespace TensileTestingApp.Services.Abstractions
{
    public interface IDataParser
    {
        TensileTestData ParseWithoutChecksum(string data);
        TensileTestData ParseWithChecksum(string data);
    }
}
