import React, { useState, useMemo } from 'react';
import { loadStripe } from '@stripe/stripe-js';
import { useQuery } from '@tanstack/react-query';
import { API_URL } from '../components/ApiUrl';
import { jwtDecode } from 'jwt-decode'; // Fixed import - remove the * as

import './../styles/billing.css';

const stripePromise = loadStripe("pk_test_51RQmwvDU2VGKpGD9rGhJKqYmBLaaoPkc5KvCLROi56j8ahrANvpiBm3hXuCITdiWl5H9roQ3wdyiYjrGsMjoaGg400RfisLjLm");

export function Billing() {
  const [loading, setLoading] = useState(false);
  const token = localStorage.getItem('token');

  const tenantId = useMemo(() => {
    if (!token) return null;

    try {
      const decoded = jwtDecode(token);
      console.log('Decoded JWT:', decoded); // Debug log to see the token structure
      
      // Try different possible claim names for tenant ID
      return decoded.TenantId ?? 
             decoded.tenantId ?? 
             decoded.tid ?? 
             decoded.sub ?? 
             decoded.tenant_id ?? 
             null;
    } catch (err) {
      console.error('Invalid JWT:', err);
      return null;
    }
  }, [token]);
  
  // Fetch plans from API
  const { data: plans, isLoading: plansLoading, error: plansError } = useQuery({
    queryKey: ['plans'],
    queryFn: async () => {
      const response = await fetch(`${API_URL}/tenant/tiers`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      
      if (!response.ok) throw new Error('Failed to fetch plans');
      return response.json();
    },
    staleTime: 60 * 60 * 1000, // Cache for 1 hour
  });

  // Fetch tenant info (tier and state)
  const { data: tenant, isLoading: tenantLoading, error: tenantError } = useQuery({
    queryKey: ['tenant'],
    queryFn: async () => {
      const response = await fetch(`${API_URL}/tenant`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!response.ok) throw new Error('Failed to fetch tenant info');
      return response.json();
    },
  });

  const handleSubscribe = async (id, priceId) => {
    setLoading(true);
    const token = localStorage.getItem('token');
    
    try {
      // Call your backend API to create a checkout session
      //Change ngrok domain
      const response = await fetch('http://billing/api/stripe/create-checkout-session', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          priceId: priceId,
          tenantId: tenantId,
          id: id.toString(),
        }),
      });
      
      const session = await response.json();
      
      // Redirect to Stripe Checkout
      const stripe = await stripePromise;
      await stripe.redirectToCheckout({
        sessionId: session.id,
      });
    } catch (error) {
      console.error('Error:', error);
    } 
  };

  // Debug log to check tenantId
  console.log('Current tenantId:', tenantId);

  if (plansLoading || tenantLoading) {
    return (
      <div className="App">
        <header>
          <h1>Choose Your Subscription Plan</h1>
        </header>
        <div className="loading-container">
          <div className="loading-spinner"></div>
          <p>Loading...</p>
        </div>
      </div>
    );
  }

  if (plansError || tenantError) {
    return (
      <div className="App">
        <header>
          <h1>Choose Your Subscription Plan</h1>
        </header>
        <div className="error-container">
          <p>There was an error loading data. Please try again later.</p>
          <button onClick={() => window.location.reload()} className="retry-button">Retry</button>
        </div>
      </div>
    );
  }

  let warningMessage = null;
  let displayActivePlan = false;
  let deploymentSuccess = false;
  
  if (tenant?.state === 1) {
    warningMessage = <p className="warning">Waiting for payment</p>;
  } else if (tenant?.state === 3) {
    warningMessage = <p className="warning cancelled">Cancelled</p>;
  } else if (tenant?.state === 4) {
    deploymentSuccess = true;
    warningMessage = <p className="success">Deployment Successful!</p>;
  } else {
    displayActivePlan = true;
  }

  return (
    <div className="App">
      <header>
        <h1>Choose Your Subscription Plan</h1>
        {warningMessage}
      </header>
      
      <div className="pricing-container">
        {deploymentSuccess ? (
          <div className="success-deployment bg-white dark:bg-gray-800 rounded-xl shadow-lg border border-gray-200 dark:border-gray-700">
            <svg xmlns="http://www.w3.org/2000/svg" className="success-icon text-green-500 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
                        
            <p className="success-description text-gray-600 dark:text-gray-400">
              You can now access your ContentRus sites using the following links:
            </p>
            
            <div className="deployment-links">
              <div 
                className="link-item bg-gray-50 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800"
                onClick={() => window.open(`https://www.contentrus.hostedsite.t${tenantId}.com`, '_blank', 'noopener,noreferrer')}
              >
                <h3 className="text-gray-700 dark:text-gray-300">Hosted Site</h3>
              </div>
              
              <div 
                className="link-item bg-gray-50 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800"
                onClick={() => window.open(`https://www.contentrus.manager.t${tenantId}.com`, '_blank', 'noopener,noreferrer')}
              >
                <h3 className="text-gray-700 dark:text-gray-300">Content Manager</h3>
              </div>
            </div>
          </div>
        ) : (
          <div className="pricing-grid">
            {plans && plans.map((plan) => {
              const isActive = tenant?.tier === plan.id && displayActivePlan;

              return (
                <div
                  key={plan.id}
                  className={`pricing-card ${isActive ? 'active-plan' : ''}`}
                >
                  <h2>{plan.name}</h2>
                  <div className="price">
                    <span className="amount">${plan.price}</span>
                    <span className="interval">/month</span>
                  </div>
                  <ul className="features">
                    {plan.features.map((feature, index) => (
                      <li key={index}>{feature}</li>
                    ))}
                  </ul>
                  <button
                    className="subscribe-button"
                    onClick={() => handleSubscribe(plan.id, plan.priceId)}
                    disabled={loading || isActive}
                  >
                    {loading ? 'Processing...' : isActive ? 'Current Plan' : 'Subscribe'}
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}