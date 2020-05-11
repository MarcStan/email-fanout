using EmailFanout.Logic;
using EmailFanout.Logic.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout
{
    public class Functions
    {
        private readonly IEmailService _emailService;
        private readonly ISendgridEmailParser _sendgridEmailParser;

        public Functions(
            IEmailService emailService,
            ISendgridEmailParser sendgridEmailParser)
        {
            _emailService = emailService;
            _sendgridEmailParser = sendgridEmailParser;
        }

        /// <summary>
        /// Called by sendgrid when a new email is received
        /// </summary>
        [FunctionName("receive")]
        public async Task<IActionResult> ReceiveAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var request = EmailRequest.Parse(req.Body, _sendgridEmailParser))
                {
                    if (await _emailService.ProcessMailAsync(request, cancellationToken))
                    {
                        return new OkResult();
                    }
                    // one or more actions failed. let sendgrid handle the retry
                    return new BadRequestResult();
                }
            }
            catch (Exception e)
            {
                log.LogCritical(e, "Failed to process request!");
                return new BadRequestResult();
            }
        }
    }
}
