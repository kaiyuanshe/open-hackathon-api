﻿using System;
using System.ComponentModel.DataAnnotations;

namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    /// <summary>
    /// Pagination parameters. Usually only `top` needed for the initial request 
    /// where a response with a `nextLink` will be returned. The `nextLink` has all 
    /// pagination parameters included, clients just request the `nextLink` without any
    /// modification to get more results.
    /// </summary>
    public class Pagination
    {
        /// <summary>
        /// conbined with <b>nr</b> to query more results. Used by server-side paging only. Don't set it manually. You should request the <b>nextLink</b> in result to request more.
        /// </summary>
        [MaxLength(200)]
        public string np { get; set; }

        /// <summary>
        /// conbined with <b>np</b> to query more results. Used by server-side paging only. Don't set it manually. You should request the <b>nextLink</b> in result to request more.
        /// </summary>
        [MaxLength(128)]
        public string nr { get; set; }

        /// <summary>
        /// Return only the top N records. any integer beween 1-1000. 100 by default. 
        /// </summary>
        /// <example>20</example>
        [Range(1, 1000)]
        public int? top { get; set; }

        public string? ToContinuationToken()
        {
            // np/nr in nextLink shouldn't be null or empty.
            if (string.IsNullOrWhiteSpace(np) || string.IsNullOrWhiteSpace(nr))
            {
                return null;
            }

            return np + " " + nr;
        }

        private static readonly char[] ContinuationTokenSplit = new char[1] { ' ' };
        public static Pagination FromContinuationToken(string? continuationToken, int? top = null)
        {
            if (continuationToken == null || continuationToken.Length <= 1)
            {
                return new Pagination { top = top };
            }

            string[] array = continuationToken.Split(ContinuationTokenSplit, 2);
            if (array.Length < 2)
            {
                return new Pagination { top = top };
            }

            return new Pagination
            {
                np = array[0],
                nr = array[1],
                top = top
            };
        }
    }
}
