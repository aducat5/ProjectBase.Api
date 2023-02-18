﻿using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Sevgi.Data.Utilities;
using Sevgi.Model;
using Sevgi.Model.Utilities;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Sevgi.Data.Services
{
    public interface IAuthService
    {
        Task<string> SignUp(string email, string password);
        Task<string> SignIn(string email, string password);
        Task<string> ExternalAuth(AuthRequest request);
        Task<string> SignOut(string email);
        Task ClearUsers();

    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<User> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }


        public async Task<string> SignUp(string email, string password)
        {
            var userToCheck = await _userManager.FindByEmailAsync(email);
            if (userToCheck is not null) throw new UserExistsException($"There already is a user registered with email: {userToCheck.Email}.");

            var userToRegister = new User()
            {
                UserName = email,
                Email = email,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(userToRegister, password);

            if (!result.Succeeded) throw new UserException($"User with email: {email} cannot be registered. Errors: {GetErrorsText(result.Errors)}");

            return await SignIn(email, password);
        }
        public async Task<string> SignUp(User user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded) throw new UserException($"User with phone: {user.PhoneNumber} cannot be registered. Errors: {GetErrorsText(result.Errors)}");

            return await SignIn(user.Email!, password);
        }

        public async Task<string> SignIn(string email, string password)
        {
            var userToCheck = await _userManager.FindByEmailAsync(email);
            if (userToCheck is null) throw new UserNotFoundException($"There is no such user with email: {email}");

            if (!await _userManager.CheckPasswordAsync(userToCheck, password)) throw new InvalidPasswordException($"Unable to authenticate user {email}");

            var authClaims = new List<Claim>
            {
                new(ClaimTypes.Email, email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = GetToken(authClaims);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private JwtSecurityToken GetToken(IEnumerable<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

            return token;
        }
        private static string GetErrorsText(IEnumerable<IdentityError> errors)
        {
            return string.Join(", ", errors.Select(error => error.Description).ToArray());
        }

        public async Task<string> ExternalAuth(AuthRequest request)
        {
            switch (request.Provider)
            {
                case AuthProviders.GOOGLE:
                    //verify google
                    var payload = await VerifyGoogleToken(request.IdToken);
                    if (payload == null) throw new InvalidTokenException("Google token is invalid, cannot authorize.");

                    //check if registered
                    var googleLoginInfo = new UserLoginInfo(request.Provider.ToString(), payload.Subject, "Google");
                    var userFromGoogle = await _userManager.FindByEmailAsync(payload.Email);

                    //register if not
                    string result;
                    if (userFromGoogle == null) result = await SignUp(payload.Email, payload.Email.GeneratePassword());
                    else result = await SignIn(payload.Email, payload.Email.GeneratePassword());

                    userFromGoogle = userFromGoogle is null ? await _userManager.FindByEmailAsync(payload.Email) : userFromGoogle;

                    //add login
                    await _userManager.AddLoginAsync(userFromGoogle!, googleLoginInfo);
                    return result;

                case AuthProviders.FIREBASE:

                    //verify firebase token
                    var firebaseToken = await FirebaseAuth.DefaultInstance!.VerifyIdTokenAsync(request.IdToken);
                    if (firebaseToken is null) throw new InvalidTokenException();

                    var firebaseUser = await FirebaseAuth.DefaultInstance.GetUserAsync(firebaseToken.Uid);
                    if (firebaseUser is null) throw new InvalidTokenException();

                    //check if registered
                    var firebaseLoginInfo = new UserLoginInfo(request.Provider.ToString(), firebaseUser.Uid, "Firebase");
                    var userFromFirebase = await _userManager.FindByLoginAsync(firebaseLoginInfo.LoginProvider, firebaseLoginInfo.ProviderKey);

                    //register if not
                    var userToRegister = new User()
                    {
                        UserName = firebaseUser.Uid.GenerateUsernameForFirebase(firebaseUser.PhoneNumber),
                        Email = firebaseUser.Uid.GenerateEmailForFirebase(),
                        PhoneNumber = firebaseUser.PhoneNumber,
                        SecurityStamp = Guid.NewGuid().ToString()
                    };

                    //register if not
                    string firebaseResult;
                    if (userFromFirebase == null) firebaseResult = await SignUp(userToRegister, firebaseUser.Uid.GeneratePassword());
                    else firebaseResult = await SignIn(userToRegister.Email, firebaseUser.Uid.GeneratePassword());

                    userFromFirebase = userFromFirebase is null ? await _userManager.FindByEmailAsync(userToRegister.Email) : userFromFirebase;

                    //add login
                    await _userManager.AddLoginAsync(userFromFirebase!, firebaseLoginInfo);
                    return firebaseResult;

                case AuthProviders.INTERNAL:
                case AuthProviders.APPLE:
                default:
                    throw new Exception("The option you requested is not supported, please use available options.");
            }
        }

        public Task<string> SignOut(string email)
        {
            throw new NotImplementedException();
        }

        public Task ClearUsers()
        {
            throw new NotImplementedException();
        }

        private async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings() { Audience = new List<string>() { _configuration["Authentication:Google:ClientId"] } };
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return payload;
        }
    }
}