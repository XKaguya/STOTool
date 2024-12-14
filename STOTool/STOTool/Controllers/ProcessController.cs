using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTool.Core;
using STOTool.Generic;
using STOTool.Settings;

namespace STOTool.Controllers
{
    [Route("[controller]")]
    public class ProcessController : Controller
    {
        [Authorize]
        [HttpGet("GetLogs")]
        public async Task<IActionResult> GetLogs()
        {
            try
            {
                if (System.IO.File.Exists(Logger.LogFilePath))
                {
                    var logs = await System.IO.File.ReadAllTextAsync(Logger.LogFilePath);
                    return Ok(logs);
                }
                else
                {
                    return NotFound("Log file not found.");
                }
            }
            catch (IOException ioEx)
            {
                return StatusCode(500, $"Error reading log file: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        
        [HttpPost("ChangePassword")]
        public IActionResult ChangePassword([FromBody] Class.ChangeAuthInfo changeAuthInfo)
        {
            try
            {
                string filePath = "auth.xml";

                var authData = XElement.Load(filePath);
                var savedUsername = authData.Element("Username")?.Value;
                var savedPassword = authData.Element("Password")?.Value;
                
                Logger.Info($"Username = {changeAuthInfo.Username}, Password = {changeAuthInfo.Password}");
                Logger.Info($"Username = {changeAuthInfo.NewUsername}, Password = {changeAuthInfo.NewPassword}");

                if (changeAuthInfo.Username == savedUsername && changeAuthInfo.Password == savedPassword)
                {
                    if (changeAuthInfo.Username == changeAuthInfo.NewUsername)
                    {
                        var alterAuthData = new XElement("AuthData",
                            new XElement("Username", savedUsername),
                            new XElement("Password", changeAuthInfo.NewPassword)
                        );
                        alterAuthData.Save(filePath);
                    }
                    else
                    {
                        var alterAuthData = new XElement("AuthData",
                            new XElement("Username", changeAuthInfo.NewUsername),
                            new XElement("Password", changeAuthInfo.NewPassword)
                        );
                        alterAuthData.Save(filePath);
                    }

                    return Json(new { success = true, message = "Password change successful." });
                }

                return Json(new { success = false, message = "Password change failed." });
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ex.StackTrace);
            }
            
            return Json(new { success = false, message = "Password change failed." });
        }
        
        [Authorize]
        [HttpPost("ApplySettings")]
        public IActionResult ApplySettings([FromForm] Class.Settings settings)
        {
            try
            {
                if (string.IsNullOrEmpty(settings.ProgramLevel))
                {
                    return BadRequest("ProgramLevel cannot be empty.");
                }

                if (string.IsNullOrEmpty(settings.LogLevel))
                {
                    return BadRequest("LogLevel cannot be empty.");
                }

                if (!settings.AutoUpdate.HasValue)
                {
                    return BadRequest("AutoUpdate cannot be null.");
                }

                if (!settings.WebSocketListenerPort.HasValue)
                {
                    return BadRequest("WebSocketListenerPort cannot be null.");
                }

                if (string.IsNullOrEmpty(settings.WebSocketListenerAddress))
                {
                    return BadRequest("WebSocketListenerAddress cannot be empty.");
                }

                if (!settings.UserInterfaceWebSocketPort.HasValue)
                {
                    return BadRequest("UserInterfaceWebSocketPort cannot be null.");
                }
                
                GlobalVariables.ProgramLevel = settings.ProgramLevel;
                GlobalVariables.LogLevel = settings.LogLevel;
                GlobalVariables.AutoUpdate = settings.AutoUpdate.Value;
                
                if (!string.IsNullOrEmpty(settings.CacheLifeTime))
                {
                    GlobalVariables.CacheLifeTime = settings.CacheLifeTime
                        .Split(',')
                        .Select(value => ushort.TryParse(value.Trim(), out var result) ? result : (ushort)0)
                        .ToArray();
                }
                
                GlobalVariables.WebSocketListenerPort = settings.WebSocketListenerPort.Value;
                GlobalVariables.WebSocketListenerAddress = settings.WebSocketListenerAddress;
                GlobalVariables.UserInterfaceWebSocketPort = settings.UserInterfaceWebSocketPort.Value;

                Api.SaveSettingsToLocalFile(Api.ConfigFilePath);
                
                return Json(new { success = true, message = "Settings applied successfully!" });
            }
            catch (IOException ioEx)
            {
                Logger.Error($"Error applying settings: {ioEx.Message}");
                return Json(new { success = false, message = "Failed to apply settings!" });
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ex.StackTrace);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        
        [Authorize]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View("Index");
        }
        
        [HttpGet("Authentication")]
        public IActionResult Authentication()
        {
            return View("Authentication");
        }
        
        [HttpPost("ValidateCredentials")]
        public async Task<IActionResult> ValidateCredentials([FromForm] Class.AuthInfo authInfo)
        {
            string filePath = "auth.xml";
            
            if (!System.IO.File.Exists(filePath))
            {
                var defaultUsername = "admin";
                var defaultPassword = "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=";
                
                var authData = new XElement("AuthData",
                    new XElement("Username", defaultUsername),
                    new XElement("Password", defaultPassword)
                );
                
                authData.Save(filePath);
                
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, authInfo.Username)
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties();

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
                
                Logger.Info($"Auth data saved to: {filePath}");
                return Json(new { success = true, message = "Login successful with default credentials!" });
            }
            else
            {
                var authData = XElement.Load(filePath);
                var savedUsername = authData.Element("Username")?.Value;
                var savedPassword = authData.Element("Password")?.Value;
                
                if (authInfo.Username == savedUsername && authInfo.Password == savedPassword)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, authInfo.Username)
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties();

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                    
                    Logger.Info($"User {savedUsername} passed.");
                    return Json(new { success = true, message = "Login successful!" });
                }
                else
                {
                    ViewData["Error"] = "Invalid Username or Password!";
                    Logger.Info($"User {savedUsername} denied.");
                    return Json(new { success = false, message = "Invalid Username or Password!" });
                }
            }
        }
        
        [Authorize]
        [HttpGet("GetElapsedTime")]
        public IActionResult GetElapsedTime()
        {
            TimeSpan elapsedTime = DateTime.Now - GlobalStaticVariables.InitializedDateTime;
            
            string formattedElapsedTime = string.Format("{0:D2}:{1:D2}:{2:D2}", 
                elapsedTime.Hours, 
                elapsedTime.Minutes, 
                elapsedTime.Seconds);
            
            return Ok(formattedElapsedTime);
        }
        
        [Authorize]
        [HttpGet("Settings")]
        public IActionResult Settings()
        {
            Class.Settings settings = new Class.Settings
            {
                ProgramLevel = GlobalVariables.ProgramLevel,
                LogLevel = GlobalVariables.LogLevel,
                AutoUpdate = GlobalVariables.AutoUpdate,
                CacheLifeTime = GlobalVariables.CacheLifeTime != null 
                    ? string.Join(",", GlobalVariables.CacheLifeTime)
                    : null,
                WebSocketListenerPort = GlobalVariables.WebSocketListenerPort,
                WebSocketListenerAddress = GlobalVariables.WebSocketListenerAddress,
                UserInterfaceWebSocketPort = GlobalVariables.UserInterfaceWebSocketPort
            };

            if (!settings.IsParameterNull())
            {
                return View("Settings", settings);
            }

            return StatusCode(500, "Error getting settings.");
        }
    }
}