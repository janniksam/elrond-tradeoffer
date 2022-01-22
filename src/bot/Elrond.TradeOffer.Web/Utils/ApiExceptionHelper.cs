namespace Elrond.TradeOffer.Web.Utils
{
    public static class ApiExceptionHelper
    {
        private static readonly ApiError[] PossibleErrors = Enum.GetValues<ApiError>().Where(p => p != ApiError.All).ToArray();
        
        // https://github.com/TelegramBotAPI/errors
        private static readonly Dictionary<ApiError, string> ApiErrorsMapping = new()
        {
            { ApiError.ChatNotFound, "Bad Request: chat not found" },
            { ApiError.UserNotFound, "Bad request: user not found" },
            { ApiError.UserIsDeactivated, "Forbidden: user is deactivated" },
            { ApiError.BotWasKicked, "Forbidden: bot was kicked" },
            { ApiError.BotBlockedByUser, "Forbidden: bot blocked by user" }
        };

        public static async Task RunAndSupressAsync(Func<Task> action, ApiError errorsToSupress = ApiError.All)
        {
            var messagesToSupress = GetMessagesToSupress(errorsToSupress).ToArray();
            try
            {
                await action();
            }
            catch (Exception e) when (messagesToSupress.Any(p => p == e.Message))
            {
                // supress
            }
            catch (Exception ex)
            {
                LoggingFactory.LogFactory?.CreateLogger(typeof(ApiExceptionHelper)).LogError(ex, "Unhandled exception occured");
            }
        }

        private static IEnumerable<string> GetMessagesToSupress(ApiError errorsToSupress)
        {
            if (errorsToSupress == ApiError.All)
            {
                errorsToSupress = PossibleErrors.Aggregate((a, b) => a | b);
            }

            foreach (var apiError in PossibleErrors)
            {
                if (errorsToSupress.HasFlag(apiError))
                {
                    yield return ApiErrorsMapping[apiError];
                }
            }
        }
    }
}
