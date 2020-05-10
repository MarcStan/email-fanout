using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EmailFanout
{
    public class FanoutFunction
    {

        /// <summary>
        /// Called internally by the queue of to be processed emails.
        /// </summary>
        [FunctionName("fanout")]
        public static async Task<IActionResult> FanoutAsync(
            [QueueTrigger("")] object queueMessage,
            ILogger log,
            CancellationToken cancellationToken)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                log.LogCritical(e, "Failed to process request!");
                return new BadRequestResult();
            }
        }
    }
}
