import { Link, NavLink, type LinkProps, type NavLinkProps } from 'react-router-dom';
import { withTenantSearch } from '../lib/publicTenant';

export function TenantLink({ to, ...rest }: LinkProps) {
  const resolved =
    typeof to === 'string'
      ? withTenantSearch(to)
      : typeof to === 'object' && to !== null && 'pathname' in to && typeof to.pathname === 'string'
        ? { ...to, pathname: withTenantSearch(to.pathname) }
        : to;
  return <Link to={resolved} {...rest} />;
}

export function TenantNavLink({ to, ...rest }: NavLinkProps) {
  const resolved = typeof to === 'string' ? withTenantSearch(to) : to;
  return <NavLink to={resolved} {...rest} />;
}
