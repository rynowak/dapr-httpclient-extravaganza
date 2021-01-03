// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace BankApp
{
    using System;
    using System.Threading.Tasks;
    using Dapr;
    using Dapr.Client;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Sample showing Dapr integration with controller.
    /// </summary>
    [ApiController]
    public class AcccountController : ControllerBase
    {
        /// <summary>
        /// State store name.
        /// </summary>
        public const string StoreName = "statestore";
        
        private readonly ILogger<AcccountController> logger;

        /// <summary>
        /// SampleController Constructor with logger injection
        /// </summary>
        /// <param name="logger"></param>
        public AcccountController(ILogger<AcccountController> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Gets the account information as specified by the id.
        /// </summary>
        /// <param name="account">Account information for the id from Dapr state store.</param>
        /// <returns>Account information.</returns>
        [HttpGet("accounts/{account}")]
        public ActionResult<Account> Get([FromState(StoreName)] StateEntry<Account> account)
        {
            if (account.Value is null)
            {
                return this.NotFound();
            }

            return account.Value;
        }

        /// <summary>
        /// Method for depositing to account as specified in transaction.
        /// </summary>
        /// <param name="transaction">Transaction info.</param>
        /// <param name="daprClient">State client to interact with Dapr runtime.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        ///  "pubsub", the first parameter into the Topic attribute, is name of the default pub/sub configured by the Dapr CLI.
        [Topic("pubsub", "deposit")]
        [HttpPost("deposit")]
        public async Task<ActionResult<Account>> Deposit(Transaction transaction, [FromServices] DaprClient daprClient)
        {
            var state = await daprClient.GetStateEntryAsync<Account>(StoreName, transaction.Id);
            state.Value ??= new Account() { Id = transaction.Id, };
            state.Value.Balance += transaction.Amount;
            await state.SaveAsync();
            return state.Value;
        }

        /// <summary>
        /// Method for withdrawing from account as specified in transaction.
        /// </summary>
        /// <param name="transaction">Transaction info.</param>
        /// <param name="daprClient">State client to interact with Dapr runtime.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        ///  "pubsub", the first parameter into the Topic attribute, is name of the default pub/sub configured by the Dapr CLI.
        [Topic("pubsub", "withdraw")]
        [HttpPost("withdraw")]
        public async Task<ActionResult<Account>> Withdraw(Transaction transaction, [FromServices] DaprClient daprClient)
        {
            var state = await daprClient.GetStateEntryAsync<Account>(StoreName, transaction.Id);
            if (state.Value == null)
            {
                return this.NotFound();
            }

            state.Value.Balance -= transaction.Amount;
            if (state.Value.Balance < 0m)
            {
                ModelState.AddModelError("amount", "not enough funds available");
                return ValidationProblem(ModelState);
            }

            await state.SaveAsync();
            return state.Value;
        }
    }
}
