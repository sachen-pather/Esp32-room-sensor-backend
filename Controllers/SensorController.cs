using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Esp32_room_sensor_data.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Esp32_room_sensor_data.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorController : ControllerBase
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly ILogger<SensorController> _logger;

        public SensorController(CosmosClient cosmosClient, Container container, ILogger<SensorController> logger)
        {
            _cosmosClient = cosmosClient;
            _container = container; // Now injected directly
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int limit = 100)
        {
            try
            {
                // Limit the number of items to retrieve
                if (limit <= 0 || limit > 1000)
                {
                    limit = 100; // Default to 100 if an invalid limit is provided
                }

                _logger.LogInformation("Retrieving sensor data, limit: {Limit}", limit);

                // Query to get the most recent sensor data entries
                var query = new QueryDefinition(
                    "SELECT * FROM c ORDER BY c.timestamp DESC OFFSET 0 LIMIT @limit")
                    .WithParameter("@limit", limit);

                var iterator = _container.GetItemQueryIterator<SensorData>(query);

                var results = new List<SensorData>();
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }

                _logger.LogInformation("Retrieved {Count} sensor data records", results.Count);
                return Ok(results);
            }
            catch (CosmosException ex)
            {
                // Log Cosmos DB error
                _logger.LogError(ex, "Cosmos DB error: {Message} - StatusCode: {StatusCode}",
                    ex.Message, ex.StatusCode);
                return StatusCode((int)ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                // Log unexpected error
                _logger.LogError(ex, "Unexpected error retrieving data: {Message}", ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorData? sensorData)
        {
            try
            {
                // Validate null or empty body
                if (sensorData == null)
                {
                    _logger.LogInformation("Received empty or null request body");
                    return Ok("Endpoint reached, no data provided");
                }

                // Ensure the ID is set for the sensor data
                if (string.IsNullOrEmpty(sensorData.Id))
                    sensorData.Id = Guid.NewGuid().ToString();

                // Validate Timestamp (Partition Key)
                if (string.IsNullOrEmpty(sensorData.Timestamp))
                {
                    _logger.LogWarning("Timestamp is missing");
                    return BadRequest("Timestamp is required");
                }

                // Log the received data
                _logger.LogInformation("Received data: {Data}", JsonConvert.SerializeObject(sensorData));

                // Insert the sensor data into the Cosmos DB container with the Timestamp as the partition key
                await _container.CreateItemAsync(sensorData, new PartitionKey(sensorData.Timestamp));
                return Ok("Data saved successfully");
            }
            catch (CosmosException ex)
            {
                // Log Cosmos DB error
                _logger.LogError(ex, "Cosmos DB error: {Message} - StatusCode: {StatusCode} - SubStatusCode: {SubStatusCode}",
                    ex.Message, ex.StatusCode, ex.SubStatusCode);
                return StatusCode((int)ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                // Log unexpected error
                _logger.LogError(ex, "Unexpected error saving data: {Message}", ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}