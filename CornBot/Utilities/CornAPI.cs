using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CornBot.Utilities
{
    public class CornAPI(HttpClient client, AppJsonSerializerContext jsc)
    {
        public HttpClient Client { get; private set; } = client;
        public AppJsonSerializerContext JSC { get; private set; } = jsc;

        public async Task<TRes> GetModelAsync<TRes>(string url)
        {
            var response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var model = await response.Content.ReadFromJsonAsync<TRes>();
            return model == null ? throw new Exception($"Failed to deserialize model from GET request to {url}.") : model;
        }

        public async Task<TRes> PostModelAsync<TReq, TRes>(string url, TReq model)
        {
            var json = JsonSerializer.Serialize(model, typeof(TReq), JSC);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await Client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            var responseModel = await response.Content.ReadFromJsonAsync<TRes>();
            return responseModel == null ? throw new Exception($"Failed to deserialize model from POST request to {url}.") : responseModel;
        }

        public async Task<TRes> PostModelAsync<TRes>(string url)
        {
            var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            var response = await Client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            var model = await response.Content.ReadFromJsonAsync<TRes>();
            return model == null ? throw new Exception($"Failed to deserialize model from POST request to {url}.") : model;
        }
    }
}
