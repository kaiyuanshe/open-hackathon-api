﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Kaiyuanshe.OpenHackathon.Server.Models.Validations
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RemoteConfigPolicyAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                // leave it to RequiredAttribute
                return ValidationResult.Success;
            }

            Template template = (Template)value;
            List<string> invalidMembers = new List<string>();
            if (template.ingressProtocol != null && template.ingressProtocol == IngressProtocol.vnc)
            {
                if (template.vnc == null)
                {
                    invalidMembers.Add(nameof(Template.vnc));
                    return new ValidationResult("vnc config is required.", invalidMembers);
                }
                if (template.vnc.userName == null)
                {
                    invalidMembers.Add(nameof(VncSettings.userName));
                }
                if (template.vnc.password == null)
                {
                    invalidMembers.Add(nameof(VncSettings.password));
                }
                if (invalidMembers.Count > 0)
                {
                    return new ValidationResult("vnc config is invalid. Missing required members.", invalidMembers);
                }
            }

            return ValidationResult.Success;
        }
    }
}
