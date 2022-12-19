﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using UT4MasterServer.Models;
using UT4MasterServer.Services;

namespace UT4MasterServer.Controllers
{
	[ApiController]
	[Route("account/api")]
	public class AccountController : ControllerBase
	{
		private readonly ILogger<AccountController> logger;
		private readonly AccountService accountService;
		private readonly SessionService sessionService;

		public AccountController(AccountService accountService, SessionService sessionService, ILogger<AccountController> logger)
		{
			this.logger = logger;
			this.accountService = accountService;
			this.sessionService = sessionService;
		}

		// IMPORATANT TODO: all methods which have parameter accessToken need to retrieve the value from Authorization header.
		//                  resources: https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Authorization
		//                             https://devblogs.microsoft.com/dotnet/bearer-token-authentication-in-asp-net-core/
		//                  we need to figure out how to use [Authorize] attribute. header is composed out of two parts <bearer|basic> <token>.
		//                  if it starts with "bearer" then <token> is the value of AccessToken.
		//                  if it starts with "basic" then <token> is composed of ClientID and ClientSecret. these can be parsed with ClientIdentification class.

		#region ACCOUNT LISTING API

		[HttpGet]
		[Route("public/account/{id}")]
		public async Task<ActionResult<string>> GetAccount(EpicID id)
		{
			logger.Log(LogLevel.Information, $"Looking for account {id}");
			var account = await accountService.GetAccountAsync(id);
			if (account == null)
				return NotFound();

			var obj = new JObject();
			obj.Add("id", account.ID.ToString());
			obj.Add("displayName", account.Username);
			obj.Add("name", $"{account.Username}"); // fake a random one
			obj.Add("email", $"{account.ID}@{Request.Host}"); // fake a random one
			obj.Add("failedLoginAttempts", 0);
			obj.Add("lastLogin", account.LastLogin.ToStringISO());
			obj.Add("numberOfDisplayNameChanges", 0);
			obj.Add("ageGroup", "UNKNOWN");
			obj.Add("headless", false);
			obj.Add("country", "SI"); // two letter country code
			obj.Add("lastName", $"{account.Username}"); // fake a random one
			obj.Add("preferredLanguage", "en");
			obj.Add("canUpdateDisplayName", true);
			obj.Add("tfaEnabled", true);
			obj.Add("emailVerified", true);
			obj.Add("minorExpected", false);
			obj.Add("minorStatus", "UNKNOWN");
			obj.Add("cabinedMode", false);
			obj.Add("hasHashedEmail", false);

			return obj.ToString(Newtonsoft.Json.Formatting.None);
		}

		[HttpGet]
		[Route("public/account")]
		public async Task<ActionResult<string>> GetAccounts([FromQuery(Name = "accountId")] List<EpicID> accountIDs)
		{
			logger.LogInformation($"List accounts: {string.Join(", ", accountIDs)}");

			// TODO: remove duplicates from accountIDs
			var accounts = await accountService.GetAccountsAsync(accountIDs);
			if (accounts is null)
				return NotFound();


			// create json response
			JArray arr = new JArray();
			foreach (var account in accounts)
			{
				var obj = new JObject();
				obj.Add("id", account.ID.ToString());
				obj.Add("displayName", account.Username);
				//if (account.ID == ???)
				{
					// this is returned only when you ask about yourself
					obj.Add("minorVerified", false);
					obj.Add("minorStatus", "UNKNOWN");
					obj.Add("cabinedMode", false);
				}
				obj.Add("externalAuths", new JObject());
				arr.Add(obj);
			}

			return arr.ToString(Newtonsoft.Json.Formatting.None);
		}

		#endregion

		#region UNIMPORTANT API

		[HttpGet]
		[Route("accounts/{idString}/metadata")]
		public ActionResult<string> GetMetadata(string idString)
		{
			EpicID id = new EpicID(idString);

			logger.LogInformation($"Get metadata of {id}");

			// unknown structure, but epic always seems to respond with this
			return "{}";
		}

		[HttpGet]
		[Route("public/account/{idString}/externalAuths")]
		public ActionResult<string> GetExternalAuths(string idString)
		{
			EpicID id = new EpicID(idString);

			logger.LogInformation($"Get external auths of {id}");
			// we dont really care about these, but structure for github is the following:
			/*
			[{
				"accountId": "0b0f09b400854b9b98932dd9e5abe7c5", "type": "github",
				"externalAuthId": "timiimit", "externalDisplayName": "timiimit",
				"authIds": [ { "id": "timiimit", "type": "github_login" } ],
				"dateAdded": "2018-01-17T18:58:39.831Z"
			}]
			*/
			return "[]";
		}

		[HttpGet]
		[Route("epicdomains/ssodomains")]
		public ActionResult<string> GetSSODomains()
		{
			logger.LogInformation(@"Get SSO domains");

			// epic responds with this: ["unrealengine.com","unrealtournament.com","fortnite.com","epicgames.com"]

			return "[]";
		}

		#endregion

		#region NON-EPIC API

		[HttpPost]
		[Route("create/account")]
		public async Task<NoContentResult> RegisterAccount([FromForm] string username, [FromForm] string password)
		{
			// TODO: should we also get user's email?
			await accountService.CreateAccountAsync(username, password); // TODO: this cannot fail?

			logger.LogInformation($"Registered new user: {username}");

			return NoContent();
		}

		#endregion
	}
}
