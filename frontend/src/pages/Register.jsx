import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import api from '../api/axios.js'

function Register() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ username: '', email: '', password: '' })
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleChange = (event) => {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      await api.post('/auth/register', form)
      navigate('/login')
    } catch (requestError) {
      setError(
        requestError.response?.data?.message ||
          'Unable to register. Please try again.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center py-5">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-12 col-md-6 col-lg-4">
            {/* Logo / Brand Section */}
            <div className="text-center mb-4" style={{ marginBottom: '32px' }}>
              <div
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: '64px',
                  height: '64px',
                  background: 'linear-gradient(135deg, #DC2626 0%, #B91C1C 100%)',
                  borderRadius: '16px',
                  boxShadow: '0 8px 24px rgba(220, 38, 38, 0.4)',
                  marginBottom: '16px',
                }}
              >
                <svg
                  width="32"
                  height="32"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="white"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
                  <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                  <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                </svg>
              </div>
              <h1
                style={{
                  fontSize: '32px',
                  fontWeight: '700',
                  color: 'var(--color-foreground)',
                  margin: '0 0 8px',
                  letterSpacing: '-0.5px',
                }}
              >
                Vault
              </h1>
              <p style={{ color: 'var(--color-muted)', fontSize: '14px', margin: 0 }}>
                Secure Product Management
              </p>
            </div>

            {/* Register Card */}
            <div className="card" style={{ border: 'none' }}>
              <div className="card-body">
                <h2
                  className="text-center"
                  style={{
                    fontSize: '24px',
                    fontWeight: '600',
                    marginBottom: '24px',
                    color: 'var(--color-foreground)',
                  }}
                >
                  Create Your Account
                </h2>

                {error ? (
                  <div className="alert alert-danger" role="alert">
                    {error}
                  </div>
                ) : null}

                <form onSubmit={handleSubmit}>
                  <div className="mb-3">
                    <label htmlFor="username" className="form-label">
                      Username
                    </label>
                    <input
                      id="username"
                      name="username"
                      type="text"
                      className="form-control"
                      placeholder="Choose a username"
                      value={form.username}
                      onChange={handleChange}
                      autoComplete="username"
                      minLength={3}
                      maxLength={50}
                      required
                    />
                  </div>

                  <div className="mb-3">
                    <label htmlFor="email" className="form-label">
                      Email Address
                    </label>
                    <input
                      id="email"
                      name="email"
                      type="email"
                      className="form-control"
                      placeholder="you@example.com"
                      value={form.email}
                      onChange={handleChange}
                      autoComplete="email"
                      required
                    />
                  </div>

                  <div className="mb-4">
                    <label htmlFor="password" className="form-label">
                      Password
                    </label>
                    <input
                      id="password"
                      name="password"
                      type="password"
                      className="form-control"
                      placeholder="Create a strong password"
                      value={form.password}
                      onChange={handleChange}
                      autoComplete="new-password"
                      minLength={8}
                      required
                    />
                    <small style={{ color: 'var(--color-muted)', fontSize: '12px', display: 'block', marginTop: '6px' }}>
                      Must be at least 8 characters
                    </small>
                  </div>

                  <button
                    type="submit"
                    className="btn btn-primary w-100"
                    disabled={isSubmitting}
                    style={{ marginBottom: '16px' }}
                  >
                    {isSubmitting ? (
                      <span style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <span
                          style={{
                            display: 'inline-block',
                            width: '14px',
                            height: '14px',
                            border: '2px solid rgba(255,255,255,0.3)',
                            borderTopColor: 'white',
                            borderRadius: '50%',
                            animation: 'spin 0.6s linear infinite',
                          }}
                        />
                        Creating Account...
                      </span>
                    ) : (
                      'Create Account'
                    )}
                  </button>
                </form>

                <div
                  style={{
                    textAlign: 'center',
                    paddingTop: '24px',
                    borderTop: '1px solid var(--glass-border)',
                  }}
                >
                  <p style={{ color: 'var(--color-muted)', fontSize: '14px', margin: 0 }}>
                    Already have an account?{' '}
                    <Link to="/login" style={{ fontWeight: '500' }}>
                      Sign In
                    </Link>
                  </p>
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="text-center mt-4">
              <p style={{ color: 'var(--color-muted)', fontSize: '13px', margin: 0 }}>
                By signing up, you agree to our Terms of Service
              </p>
            </div>
          </div>
        </div>
      </div>

      <style>{`
        @keyframes spin {
          to { transform: rotate(360deg); }
        }

        .min-vh-100 {
          min-height: 100vh;
        }
      `}</style>
    </div>
  )
}

export default Register
