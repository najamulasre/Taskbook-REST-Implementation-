﻿using System.ComponentModel.DataAnnotations;

namespace Najam.TaskBook.WebApi.Parameters.UserGroups
{
    public class CreateUserGroupParameters
    {
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
        public string GroupName { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }
}
