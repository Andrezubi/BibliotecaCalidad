namespace FrontEnd.Services
{
    public class AuthSessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthSessionService(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAuthenticated
        {
            get
            {
                var token = _httpContextAccessor
                    .HttpContext?
                    .Session
                    .GetString("AuthToken");

                return !string.IsNullOrEmpty(token);
            }
        }

        public string? Username =>
            _httpContextAccessor
                .HttpContext?
                .Session
                .GetString("Username");

        public string? Role =>
            _httpContextAccessor
                .HttpContext?
                .Session
                .GetString("Role");

        public bool HasRole(string role)
        {
            return string.Equals(
                Role,
                role,
                StringComparison.OrdinalIgnoreCase
            );
        }

        public bool HasAnyRole(params string[] roles)
        {
            if (string.IsNullOrEmpty(Role))
                return false;

            return roles.Any(role =>
                string.Equals(
                    Role,
                    role,
                    StringComparison.OrdinalIgnoreCase
                ));
        }
    }
}
