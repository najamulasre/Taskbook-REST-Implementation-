﻿using Microsoft.AspNetCore.Mvc;

namespace Najam.TaskBook.WebApi.Validation
{
    public class ConflictResult : ObjectResult
    {
        private const int ConflictStatusCode = 409;

        public ConflictResult(string message = "Conflict") : base(new {Reason = message})
        {
            StatusCode = ConflictStatusCode;
        }
    }
}