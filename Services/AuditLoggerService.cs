using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Pages;
using WebApplication1.Model;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Services
{
    public class AuditLoggerService
    {
        private readonly AuthDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLoggerService> _logger;

        public AuditLoggerService(AuthDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuditLoggerService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogActivityAsync(string email, string action)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var log = new AuditLog
                {
                    UserId = email,
                    Action = action,
                    IPAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error logging audit event: {ex.Message}");
            }
        }
    }
}