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

namespace daiot
{
    public class Todo
    {
        
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsCompleted { get; set; }

        
    }

    public static class TodosApi
    {
        private static List<Todo> _todos = new();
        
        [FunctionName("todos-get")]
        public static IActionResult Gets(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            
            return new OkObjectResult(_todos);
        }
        
        [FunctionName("todos-post")]
        public static IActionResult Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] Todo todo,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

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

            _todos = _todos.Where(x => x.Id != id).ToList();


            return new OkResult();
        }
        
        [FunctionName("todos-change")]
        public static IActionResult Change(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todos-change/{id}")] HttpRequest request,
            int id,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var todo = _todos.FirstOrDefault(x => x.Id == id);
            if (null != todo) todo.IsCompleted = !todo.IsCompleted;
                
            return new OkResult();
        }
    }
}
