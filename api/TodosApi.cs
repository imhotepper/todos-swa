using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace daiot
{
    public class Todo
    {
        
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
        public string User { get; set; }

        
    }


    public class ClientPrincipal
  {
      public string IdentityProvider { get; set; }
      public string UserId { get; set; }
      public string UserDetails { get; set; }
      public IEnumerable<string> UserRoles { get; set; }
  }

 

    public static class TodosApi
    {
        private static List<Todo> _todos = new();

        public static ClaimsPrincipal GetCurrentUser(this HttpRequest req){

            var principal = new ClientPrincipal();

      if (req.Headers.TryGetValue("x-ms-client-principal", out var header))
      {
          var data = header[0];
          var decoded = Convert.FromBase64String(data);
          var json = Encoding.UTF8.GetString(decoded);
          principal = JsonConvert.DeserializeObject<ClientPrincipal>(json);//, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
      }

      principal.UserRoles = principal.UserRoles?.Except(new string[] { "anonymous" }, StringComparer.CurrentCultureIgnoreCase);

      if (!principal.UserRoles?.Any() ?? true)
      {
          return new ClaimsPrincipal();
      }

      var identity = new ClaimsIdentity(principal.IdentityProvider);
      identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.UserId));
      identity.AddClaim(new Claim(ClaimTypes.Name, principal.UserDetails));
      identity.AddClaims(principal.UserRoles.Select(r => new Claim(ClaimTypes.Role, r)));

      return new ClaimsPrincipal(identity);
        }


        
        [FunctionName("todos-get")]
        public static IActionResult Gets(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation($"Request headers: {JsonConvert.SerializeObject(req.Headers)}");
            var user =  req.GetCurrentUser();
            
            log.LogInformation($"HTTP trigger current user: {user}");


            
            return new OkObjectResult(_todos.Where(x=>x.User == user?.Identity?.Name));
        }
        
        [FunctionName("todos-post")]
        public static async Task<IActionResult> Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var user =  req.GetCurrentUser();

            var requestBody = String.Empty;
                using (StreamReader streamReader =  new  StreamReader(req.Body))
                {
                    requestBody = await streamReader.ReadToEndAsync();
                }
    
        var todo  = JsonConvert.DeserializeObject<Todo>(requestBody);

            todo.User =user?.Identity?.Name;
            todo.Id =  _todos.Count + 1 ;
            _todos.Add(todo);

            Console.WriteLine($" todo received: {JsonConvert.SerializeObject(todo)}");
            
            return new OkObjectResult(todo);
        }
        
        [FunctionName("todos-delete")]
        public static IActionResult Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todos-delete/{id}")] HttpRequest request,
            int id,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            log.LogInformation($"HTTP trigger current user: {request.GetCurrentUser()}");

            _todos = _todos.Where(x => x.Id != id ).ToList();


            return new OkResult();
        }
        
        [FunctionName("todos-change")]
        public static IActionResult Change(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todos-change/{id}")] HttpRequest request,
            int id,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation($"HTTP trigger current user: {request.GetCurrentUser()}");
            var user =  request.GetCurrentUser();


            var todo = _todos.FirstOrDefault(x => x.Id == id && x.User == user?.Identity?.Name);
            if (null != todo) todo.IsCompleted = !todo.IsCompleted;
                
            return new OkResult();
        }
    }
}
