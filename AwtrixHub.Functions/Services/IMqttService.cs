using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AwtrixHub.Functions.Services
{
    public interface IMqttService
    {
        Task PublishAsync(string topic, string payload);


    }
}
