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
using Npgsql;
using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace daiot
{
    [Dapper.Contrib.Extensions.Table("todos")]
    public class Todo
    {
        public int Id { get; set; }
       public string Title { get; set; }
        public bool IsCompleted { get; set; }
        public string UserName { get; set; }


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
        private static string _connectionString = "";


        static TodosApi()
        {
            _connectionString = Environment.GetEnvironmentVariable("DefaultConnection");
            Console.WriteLine($"connection stirng: {_connectionString}");
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            // DapperExtensions.DapperExtensions.SqlDialect = new DapperExtensions.Sql.PostgreSqlDialect();
        }

        public static ClaimsPrincipal GetCurrentUser(this HttpRequest req)
        {

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
        public static async Task<IActionResult> Gets(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            // log.LogInformation($"Request headers: {JsonConvert.SerializeObject(req.Headers)}");
            var user = req.GetCurrentUser();

            log.LogInformation($"HTTP trigger current user: {user}");

            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                var todos = await db.QueryAsync<Todo>("select * from Todos where UserName = @username", new { username = user?.Identity?.Name ?? "" });
                return new OkObjectResult(todos);
            }
        }

        [FunctionName("todos-post")]
        public static async Task<IActionResult> Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var user = req.GetCurrentUser();

            var requestBody = String.Empty;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }

            var todo = JsonConvert.DeserializeObject<Todo>(requestBody);

            todo.UserName = user?.Identity?.Name;

            Console.WriteLine($"Saving todo: {JsonConvert.SerializeObject(todo)}");
            
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                 await db.ExecuteAsync(@"insert into todos (Title, UserName) values (@title, @userName)", new { title = todo.Title, userName = user?.Identity?.Name ?? "" });
                // var _ = await db.InsertAsync<Todo>(todo);
                return new AcceptedResult();
            }
        }

        [FunctionName("todos-delete")]
        public static async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todos-delete/{id}")] HttpRequest request,
            int id,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            log.LogInformation($"HTTP trigger current user: {request.GetCurrentUser()}");

            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                var _ = await db.ExecuteAsync("delete from todos where id = @id", new { id = id });
                return new AcceptedResult();
            }
        }

        [FunctionName("todos-change")]
        public static async Task< IActionResult> Change(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todos-change/{id}")] HttpRequest request,
            int id,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation($"HTTP trigger current user: {request.GetCurrentUser()}");
            var user = request.GetCurrentUser();

            using (IDbConnection db = new NpgsqlConnection(_connectionString)){
                await db.ExecuteAsync("update todos set isCompleted = NOT isCompleted where id = @id", new {id = id});
                // var td = await db.GetAsync<Todo>(id);
                // if (td != null){
                //     td.IsCompleted =  !td.IsCompleted;
                //     await db.UpdateAsync<Todo>(td);    
                // }

            }


            var todo = _todos.FirstOrDefault(x => x.Id == id && x.UserName == user?.Identity?.Name);
            if (null != todo) todo.IsCompleted = !todo.IsCompleted;

            return new OkResult();
        }
    }
}
