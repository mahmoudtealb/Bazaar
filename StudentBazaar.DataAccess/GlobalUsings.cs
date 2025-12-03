// ==================
// System namespaces
// ==================
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Threading.Tasks;

// ==================
// ASP.NET Core Identity (لو ApplicationDbContext : IdentityDbContext<ApplicationUser>)
// ==================
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

// ==================
// EF Core
// ==================
global using Microsoft.EntityFrameworkCore;

// ==================
// Project namespaces
// ==================

// Domain layer (Entities project)
global using StudentBazaar.Entities.Models;
global using StudentBazaar.Entities.Repositories;

// DataAccess layer
global using StudentBazaar.DataAccess;              // عشان ApplicationDbContext
global using StudentBazaar.DataAccess.Repositories; // عشان الـ Repositories جوه نفس المشروع
