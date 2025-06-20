using System;
using System.Collections.Generic;
using System.Linq;
using ContentRus.TenantManagement.Models;
using ContentRus.TenantManagement.Data;

namespace ContentRus.TenantManagement.Services
{
    public class TenantService
    {
        private readonly AppDbContext _context;

        public TenantService(AppDbContext context)
        {
            _context = context;
        }

        public Tenant CreateTenant()
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Tier = TenantTier.Basic,
                State = TenantState.Created,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            _context.SaveChanges();
            return tenant;
        }

        public bool UpdateTenantState(Guid id, TenantState newState)
        {
            var tenant = GetTenant(id);
            if (tenant == null) return false;

            tenant.State = newState;
            _context.Tenants.Update(tenant);
            _context.SaveChanges();
            return true;
        }

        public bool UpdateTenantTier(Guid id, TenantTier newTier)
        {
            var tenant = GetTenant(id);
            if (tenant == null) return false;

            tenant.Tier = newTier;
            _context.Tenants.Update(tenant);
            _context.SaveChanges();
            return true;
        }

        public bool UpdateTenantInfo(Guid id, TenantInfoDTO tenantInfo)
        {
            var tenant = GetTenant(id);
            if (tenant == null) return false;

            tenant.Name = tenantInfo.Name;
            tenant.Country = tenantInfo.Country;
            tenant.Address = tenantInfo.Address;

            _context.Tenants.Update(tenant);
            _context.SaveChanges();
            return true;
        }


        public Tenant? GetTenant(Guid id)
        {
            return _context.Tenants.FirstOrDefault(t => t.Id == id);
        }

        public IEnumerable<TenantPlan> GetAllTenantTiers()
        {
            return _context.TenantPlans;
        }

        public IEnumerable<Tenant> GetAllTenants()
        {
            return _context.Tenants;
        }
    }
}
