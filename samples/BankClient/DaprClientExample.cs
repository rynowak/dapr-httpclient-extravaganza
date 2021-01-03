using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

namespace BankClient
{
    public class DaprClientExample : Example
    {
        public override string DisplayName => "Regular DaprClient";

        public override async Task RunAsync(CancellationToken cancellationToken)
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var client = new DaprClientBuilder()
                .UseJsonSerializationOptions(options)
                .Build();

            // Scenario 1: Check if the account already exists.
            Account? account = null;
            try
            {
                account = await client.InvokeMethodAsync<Account>("bank", "accounts/17", HttpInvocationOptions.UsingGet(), cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response.HttpStatusCode == HttpStatusCode.NotFound)
            {
                // Account does not exist.
            }

            Console.WriteLine($"Scenario 1: account '17' {(account is null ? "does not exist" : "already exists")}");

            // Scenario 2: Deposit some money
            var transaction = new Transaction()
            {
                Amount = 100m,
                Id = "17",
            };

            // note: relies on default HTTP method == POST
            account = await client.InvokeMethodAsync<Transaction, Account>("bank", "/deposit", transaction, cancellationToken: cancellationToken);

            // read updated balance
            Console.WriteLine($"Scenario 2: account '17' has '{account?.Balance}' money");

            // Scenario 3: Handle a validation error without exceptions - actually you can't :(
            transaction = new Transaction()
            {
                Amount = 1_000_000m,
                Id = "17",
            };
            
            var bytes = JsonSerializer.SerializeToUtf8Bytes(transaction, options);
            try
            {
                var response = await client.InvokeMethodRawAsync("bank", "/withdraw", bytes, cancellationToken: cancellationToken);

                // We don't actually expect this example to succeed - we expect a 400
                Console.WriteLine("Something went wrong :(");
                return;
            }
            catch (InvocationException ex) when (ex.Response.HttpStatusCode == HttpStatusCode.BadRequest)
            {
                var text = Encoding.UTF8.GetString(ex.Response.Body);
                Console.WriteLine(text);

                // This actually does not work. Returning a single validation error message in the problemdetails
                // format is too large and gets truncated.
                //
                // This means the default experience for .NET developers today is that error handling just doesn't work.

                var problem = JsonSerializer.Deserialize<ProblemDetails>(ex.Response.Body, options);
                Console.WriteLine($"Scenario 3: got the following errors:");
                foreach (var kvp in problem!.Errors)
                {
                    Console.WriteLine($"{kvp.Key}: {string.Join(", ", kvp.Value)}");
                }
            }
        }
    }
}