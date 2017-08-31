using Newtonsoft.Json;
using System;

namespace LiveCodingLib.Events.Args
{
    public abstract class BaseArgs : EventArgs
    {
        [JsonProperty(PropertyName = "_id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonProperty(PropertyName = "_timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
