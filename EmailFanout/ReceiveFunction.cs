using EmailFanout.Logic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout
{
    public class ReceiveFunction
    {
        private readonly IEmailService _emailService;

        public ReceiveFunction(
            IEmailService emailService)
        {
            _emailService = emailService;
        }

        /// <summary>
        /// Called by sendgrid when a new email is received
        /// </summary>
        [FunctionName("receive")]
        public async Task<IActionResult> ReceiveAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            ILogger log,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    // body can only be read once
                    req.Body.CopyTo(stream);
                    stream.Position = 0;
                    await _emailService.ProcessMailAsync(stream, cancellationToken);
                }

                return new OkResult();
            }
            catch (Exception e)
            {
                log.LogCritical(e, "Failed to process request!");
                return new BadRequestResult();
            }
        }
    }
}
