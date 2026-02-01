using Light.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Monolith.Blazor.Extensions;
using Monolith.Blazor.Services;
using Monolith.Blazor.Shared;
using Monolith.HttpApi.Common.Interfaces;
using Monolith.HttpApi.Identity;
using System.Security.Claims;

namespace Monolith.Blazor.Infrastructure;

public class JwtAuthenticationStateProvider(
    TokenStorage tokenStorage,
    NavigationManager navigationManager,
    IServiceScopeFactory serviceScopeFactory) :
    AuthenticationStateProvider, ISignInManager, ITokenProvider
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ClaimsPrincipal? CurrentUser { get; private set; }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (CurrentUser is not null)
        {
            return new AuthenticationState(CurrentUser);
        }

        var accessToken = await GetAccessTokenAsync();

        if (!string.IsNullOrEmpty(accessToken))
        {
            await ForceLoadUserClaims(accessToken);
        }

        return new AuthenticationState(CurrentUser ?? new(new ClaimsIdentity()));
    }

    private async Task ForceLoadUserClaims(string accessToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var userProfileService = scope.ServiceProvider.GetRequiredService<UserProfileHttpService>();

        var getUserClaims = await userProfileService.GetAsync();

        if (getUserClaims.Succeeded is false)
        {
            Console.WriteLine($"Error when get user profiles");

            return;
        }

        var userClaims = getUserClaims.Data.BuildClaims();

        userClaims.AddRange(JwtExtensions.ReadClaims(accessToken));

        CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "JWT"));
    }

    public async Task<Result> SignInAsync(LoginRequest model)
    {
        // We make sure the access token is only refreshed by one thread at a time. The other ones have to wait.
        await _semaphore.WaitAsync();

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenHttpService>();

        try
        {
            var getToken = await tokenService.GetTokenAsync(model.Username, model.Password);

            if (getToken.Succeeded)
            {
                var userToken = new TokenModel(
                    getToken.Data.AccessToken,
                    getToken.Data.ExpiresIn,
                    getToken.Data.RefreshToken);

                await tokenStorage.SaveAsync(userToken);

                await ForceLoadUserClaims(userToken.Token);

                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            }

            return getToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SignOutAsync()
    {
        // We make sure the access token is only refreshed by one thread at a time. The other ones have to wait.
        await _semaphore.WaitAsync();

        try
        {
            CurrentUser = null;
            //UserToken = null;
            await tokenStorage.ClearAsync();

            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

            navigationManager.RedirectToLogin(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var userToken = await tokenStorage.GetAsync();

        if (userToken is not null && userToken.IsExpiringSoon() && !string.IsNullOrEmpty(userToken.RefreshToken))
        {
            if (userToken.IsRefreshTokenExpired() is false)
            {
                // We make sure the access token is only refreshed by one thread at a time. The other ones have to wait.
                await _semaphore.WaitAsync();

                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var tokenService1 = scope.ServiceProvider.GetRequiredService<TokenHttpService>();

                //var refreshToken = Result.Error();
                var refreshToken = await tokenService1.RefreshTokenAsync(userToken.Token, userToken.RefreshToken);

                if (refreshToken.Succeeded)
                {
                    userToken = new TokenModel(
                        refreshToken.Data.AccessToken,
                        refreshToken.Data.ExpiresIn,
                        refreshToken.Data.RefreshToken);

                    await tokenStorage.SaveAsync(userToken);

                    await ForceLoadUserClaims(userToken.Token);

                    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

                    Console.WriteLine($"Refresh token OK");
                }
                else
                {
                    Console.WriteLine($"Refresh token error: {refreshToken.Message}");
                }

                _semaphore.Release();
            }
            else
            {
                Console.WriteLine($"Refresh token is expired.");
                //await LogoutAsync();
            }
        }

        /*
        if (savedToken == null)
        {
            Console.WriteLine("Token is null");

            await LogoutAsync();

            return default;
        }
        */

        return userToken?.Token;
    }
}
