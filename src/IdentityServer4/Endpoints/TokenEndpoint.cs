﻿using IdentityServer4.Core.ResponseHandling;
using IdentityServer4.Core.Results;
using IdentityServer4.Core.Validation;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace IdentityServer4.Core.Endpoints
{
    public class TokenEndpoint : IEndpoint
    {
        private readonly ClientSecretValidator _clientValidator;
        private readonly ILogger _logger;
        private readonly TokenRequestValidator _requestValidator;
        private readonly TokenResponseGenerator _responseGenerator;

        public TokenEndpoint(TokenRequestValidator requestValidator, ClientSecretValidator clientValidator, TokenResponseGenerator responseGenerator, ILoggerFactory loggerFactory)
        {
            _requestValidator = requestValidator;
            _clientValidator = clientValidator;
            _responseGenerator = responseGenerator;
            _logger = loggerFactory.CreateLogger<TokenEndpoint>();
        }

        public async Task<IResult> ProcessAsync(HttpContext context)
        {
            _logger.LogVerbose("Start token request.");

            // validate HTTP
            if (context.Request.Method != "POST" || !context.Request.HasFormContentType)
            {
                // todo logging
                return new TokenErrorResult(Constants.TokenErrors.InvalidRequest);
            }

            // validate client
            var clientResult = await _clientValidator.ValidateAsync(context);

            if (clientResult.Client == null)
            {
                return new TokenErrorResult(Constants.TokenErrors.InvalidClient);
            }
            
            // validate request
            var requestResult = await _requestValidator.ValidateRequestAsync(
                context.Request.Form.AsNameValueCollection(), 
                clientResult.Client);

            if (requestResult.IsError)
            {
                return new TokenErrorResult(requestResult.Error, requestResult.ErrorDescription);
            }

            // create response
            var response = await _responseGenerator.ProcessAsync(_requestValidator.ValidatedRequest);

            // return result
            return new TokenResult(response);
        }
    }
}