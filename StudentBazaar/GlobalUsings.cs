// ==============================
// System
// ==============================
global using System.Diagnostics;
global using System.ComponentModel.DataAnnotations;

// ==============================
// ASP.NET Core
// ==============================
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Rendering;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.SignalR;

// ==============================
// EF Core
// ==============================
global using Microsoft.EntityFrameworkCore;

// ==============================
// Packages
// ==============================
global using Newtonsoft.Json;

// ==============================
// Project Namespaces
// ==============================

// Domain Layer (Entities)
global using StudentBazaar.Entities.Models;
global using StudentBazaar.Entities.Repositories;

// DataAccess Layer
global using StudentBazaar.DataAccess;
global using StudentBazaar.DataAccess.Repositories;

// Web Layer
global using StudentBazaar.Web.ViewModels;
global using StudentBazaar.Web.Hubs;
