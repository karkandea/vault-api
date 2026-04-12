import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/axios.js'

const pageSize = 10
const maxImageSize = 2 * 1024 * 1024
const allowedImageTypes = ['image/jpeg', 'image/png', 'image/webp']
const emptyForm = {
  id: null,
  name: '',
  description: '',
  price: '',
}

function CameraIcon({ size = 18, color = 'currentColor' }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke={color}
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M4 7h3l2-2h6l2 2h3a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2Z" />
      <circle cx="12" cy="13" r="3.5" />
    </svg>
  )
}

function ProductImage({ imageUrl, alt }) {
  if (imageUrl) {
    return (
      <img
        src={imageUrl}
        alt={alt}
        style={{
          width: '44px',
          height: '44px',
          objectFit: 'cover',
          borderRadius: '8px',
          border: '1px solid var(--color-border)',
        }}
      />
    )
  }

  return (
    <div
      aria-label="No product image"
      style={{
        width: '44px',
        height: '44px',
        borderRadius: '8px',
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'var(--color-primary)',
        border: '1px solid var(--color-border)',
        color: 'var(--color-primary-foreground)',
      }}
    >
      <CameraIcon />
    </div>
  )
}

function formatPrice(price) {
  return new Intl.NumberFormat('id-ID', {
    style: 'currency',
    currency: 'IDR',
    maximumFractionDigits: 0,
  }).format(price)
}

function formatPriceInput(value) {
  const numericValue = value.replace(/\D/g, '')
  if (!numericValue) return ''
  return new Intl.NumberFormat('id-ID').format(Number(numericValue))
}

function parseFormattedPrice(formattedValue) {
  return formattedValue.replace(/\./g, '')
}

function formatDate(value) {
  if (!value) {
    return '-'
  }

  return new Date(value).toLocaleString()
}

function ProductFormModal({
  fieldErrors = {},
  form,
  formError,
  imageError,
  imagePreviewUrl,
  isSubmitting,
  mode,
  onChange,
  onClose,
  onImageChange,
  onSubmit,
  selectedImageName,
}) {
  const title = mode === 'edit' ? 'Edit Product' : 'Add Product'
  const submitLabel = isSubmitting
    ? mode === 'edit'
      ? 'Saving...'
      : 'Creating...'
    : mode === 'edit'
      ? 'Save Changes'
      : 'Create Product'

  const nameLength = form.name.length
  const descriptionLength = form.description.length
  const maxNameLength = 200
  const maxDescriptionLength = 500

  return (
    <>
      <div
        className=""
        tabIndex="-1"
        role="dialog"
        aria-modal="true"
        aria-labelledby="product-form-modal-title"
        style={{
          position: 'fixed',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1050,
          backgroundColor: 'rgba(0,0,0,0.5)',
        }}
      >
        <div className="modal-dialog">
          <div className="modal-content">
            <form onSubmit={onSubmit}>
              <div className="modal-header">
                <h2 className="modal-title fs-5" id="product-form-modal-title">
                  {title}
                </h2>
                <button
                  type="button"
                  className="btn-close"
                  aria-label="Close"
                  onClick={onClose}
                  disabled={isSubmitting}
                />
              </div>

              <div className="modal-body">
                {formError ? (
                  <div
                    style={{
                      padding: '16px 20px',
                      borderRadius: '12px',
                      background: 'color-mix(in oklch, var(--color-destructive) 10%, transparent)',
                      border: '1px solid color-mix(in oklch, var(--color-destructive) 30%, transparent)',
                      marginBottom: '24px',
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
                      <svg
                        width="18"
                        height="18"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="#DC2626"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        style={{ flexShrink: 0, marginTop: '2px' }}
                      >
                        <circle cx="12" cy="12" r="10" />
                        <line x1="12" y1="8" x2="12" y2="12" />
                        <line x1="12" y1="16" x2="12.01" y2="16" />
                      </svg>
                      <div style={{ flex: 1 }}>
                        <div style={{ color: '#DC2626', fontSize: '14px', fontWeight: '600', marginBottom: '2px' }}>
                          Failed to {mode === 'edit' ? 'update' : 'create'} product
                        </div>
                        <div style={{ color: '#DC2626', fontSize: '13px', opacity: 0.9 }}>
                          {formError}
                        </div>
                      </div>
                    </div>
                  </div>
                ) : null}

                <div className="mb-3">
                  <label htmlFor="product-name" className="form-label">
                    Product Name
                  </label>
                  <input
                    id="product-name"
                    name="name"
                    type="text"
                    className={`form-control${fieldErrors.name ? ' is-invalid' : ''}`}
                    placeholder="Enter product name"
                    value={form.name}
                    onChange={onChange}
                    maxLength={maxNameLength}
                    required
                    disabled={isSubmitting}
                  />
                  {fieldErrors.name ? (
                    <div className="invalid-feedback d-block">Nama produk wajib diisi</div>
                  ) : null}
                  <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '4px' }}>
                    <small
                      style={{
                        fontSize: '12px',
                        color: nameLength > maxNameLength * 0.9 ? '#DC2626' : 'var(--color-muted)',
                      }}
                    >
                      {nameLength}/{maxNameLength}
                    </small>
                  </div>
                </div>

                <div className="mb-3">
                  <label htmlFor="product-description" className="form-label">
                    Description
                  </label>
                  <textarea
                    id="product-description"
                    name="description"
                    className="form-control"
                    rows="3"
                    placeholder="Enter product description (optional)"
                    value={form.description}
                    onChange={onChange}
                    maxLength={maxDescriptionLength}
                    disabled={isSubmitting}
                  />
                  <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '4px' }}>
                    <small
                      style={{
                        fontSize: '12px',
                        color: descriptionLength > maxDescriptionLength * 0.9 ? '#DC2626' : 'var(--color-muted)',
                      }}
                    >
                      {descriptionLength}/{maxDescriptionLength}
                    </small>
                  </div>
                </div>

                <div>
                  <label htmlFor="product-price" className="form-label">
                    Price (IDR)
                  </label>
                  <input
                    id="product-price"
                    name="price"
                    type="text"
                    className={`form-control${fieldErrors.price ? ' is-invalid' : ''}`}
                    placeholder="Enter product price"
                    value={form.price}
                    onChange={onChange}
                    required
                    disabled={isSubmitting}
                  />
                  {fieldErrors.price ? (
                    <div className="invalid-feedback d-block">Harga produk wajib diisi</div>
                  ) : null}
                  {formError && (formError.includes('Price') || formError.includes('100,000') || formError.includes('10,000,000')) ? (
                    <div className="invalid-feedback d-block">⚠ Harga harus antara Rp 100.000 – Rp 10.000.000</div>
                  ) : (
                    <small style={{ color: 'var(--color-muted)', fontSize: '12px', display: 'block', marginTop: '6px' }}>
                      Min: 100,000 | Max: 10,000,000
                    </small>
                  )}
                </div>

                <div className="mt-4">
                  <label htmlFor="product-image" className="form-label">
                    Product Image
                  </label>
                  <input
                    id="product-image"
                    type="file"
                    className={`form-control${imageError ? ' is-invalid' : ''}`}
                    accept=".jpg,.jpeg,.png,.webp"
                    onChange={onImageChange}
                    disabled={isSubmitting}
                  />
                  {imageError ? (
                    <div className="invalid-feedback d-block">{imageError}</div>
                  ) : null}
                  <div
                    style={{
                      marginTop: '12px',
                      padding: '12px',
                      borderRadius: '14px',
                      border: '1px solid var(--color-border)',
                      background: 'color-mix(in oklch, var(--color-muted) 10%, transparent)',
                      display: 'flex',
                      alignItems: 'center',
                      gap: '12px',
                    }}
                  >
                    <ProductImage imageUrl={imagePreviewUrl} alt="Selected product preview" />
                    <div style={{ minWidth: 0 }}>
                      <div style={{ color: 'var(--color-foreground)', fontSize: '13px', fontWeight: '500' }}>
                        {selectedImageName || (imagePreviewUrl ? 'Current image' : 'No image selected')}
                      </div>
                      <div style={{ color: 'var(--color-muted)', fontSize: '12px', marginTop: '2px' }}>
                        JPG, PNG, or WEBP up to 2MB
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  onClick={onClose}
                  disabled={isSubmitting}
                >
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                  {submitLabel}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
      <div className="modal-backdrop show" />
    </>
  )
}

function ConfirmModal({ deleteError, isSubmitting, message, onClose, onConfirm, title }) {
  return (
    <>
      <div
        className="modal d-block"
        tabIndex="-1"
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-modal-title"
        style={{
          position: 'fixed',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1050,
          backgroundColor: 'rgba(0,0,0,0.5)',
        }}
      >
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <div className="modal-header">
              <h2 className="modal-title fs-5" id="confirm-modal-title">
                {title}
              </h2>
              <button
                type="button"
                className="btn-close"
                aria-label="Close"
                onClick={onClose}
                disabled={isSubmitting}
              />
            </div>

            <div className="modal-body">
              {deleteError ? (
                <div
                  style={{
                    padding: '12px 16px',
                    borderRadius: '10px',
                    background: 'color-mix(in oklch, var(--color-destructive) 10%, transparent)',
                    border: '1px solid color-mix(in oklch, var(--color-destructive) 30%, transparent)',
                    marginBottom: '16px',
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
                    <svg
                      width="18"
                      height="18"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="#DC2626"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      style={{ flexShrink: 0, marginTop: '2px' }}
                    >
                      <circle cx="12" cy="12" r="10" />
                      <line x1="12" y1="8" x2="12" y2="12" />
                      <line x1="12" y1="16" x2="12.01" y2="16" />
                    </svg>
                    <div style={{ flex: 1 }}>
                      <div style={{ color: '#DC2626', fontSize: '14px', fontWeight: '600', marginBottom: '2px' }}>
                        Delete failed
                      </div>
                      <div style={{ color: '#DC2626', fontSize: '13px', opacity: 0.9 }}>
                        {deleteError}
                      </div>
                    </div>
                  </div>
                </div>
              ) : null}
              <p className="mb-0">{message}</p>
            </div>

            <div className="modal-footer">
              <button
                type="button"
                className="btn btn-outline-secondary"
                onClick={onClose}
                disabled={isSubmitting}
              >
                Cancel
              </button>
              <button
                type="button"
                className="btn btn-danger"
                onClick={onConfirm}
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      </div>
      <div className="modal-backdrop show" />
    </>
  )
}

function LogoutConfirmModal({ isSubmitting, onClose, onConfirm }) {
  return (
    <>
      <div
        className=""
        tabIndex="-1"
        role="dialog"
        aria-modal="true"
        aria-labelledby="logout-confirm-modal-title"
        style={{
          position: 'fixed',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1050,
          backgroundColor: 'rgba(0,0,0,0.5)',
        }}
      >
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <div className="modal-header">
              <h2 className="modal-title fs-5" id="logout-confirm-modal-title">
                Konfirmasi Logout
              </h2>
              <button
                type="button"
                className="btn-close"
                aria-label="Close"
                onClick={onClose}
                disabled={isSubmitting}
              />
            </div>

            <div className="modal-body">
              <p className="mb-0">Yakin ingin keluar?</p>
            </div>

            <div className="modal-footer">
              <button
                type="button"
                className="btn btn-outline-secondary"
                onClick={onClose}
                disabled={isSubmitting}
              >
                Batal
              </button>
              <button
                type="button"
                className="btn btn-primary"
                onClick={onConfirm}
                disabled={isSubmitting}
              >
                Keluar
              </button>
            </div>
          </div>
        </div>
      </div>
      <div className="modal-backdrop show" />
    </>
  )
}

function Products() {
  const navigate = useNavigate()
  const [products, setProducts] = useState([])
  const [totalProducts, setTotalProducts] = useState(0)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [searchName, setSearchName] = useState('')
  const [minPrice, setMinPrice] = useState('')
  const [maxPrice, setMaxPrice] = useState('')
  const [sortBy, setSortBy] = useState('')
  const [sortOrder, setSortOrder] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [filterError, setFilterError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [formMode, setFormMode] = useState(null)
  const [formError, setFormError] = useState('')
  const [fieldErrors, setFieldErrors] = useState({})
  const [productForm, setProductForm] = useState(emptyForm)
  const [selectedImageFile, setSelectedImageFile] = useState(null)
  const [selectedImageName, setSelectedImageName] = useState('')
  const [imagePreviewUrl, setImagePreviewUrl] = useState('')
  const [imageError, setImageError] = useState('')
  const [deleteTarget, setDeleteTarget] = useState(null)
  const [deleteError, setDeleteError] = useState('')
  const [showLogoutModal, setShowLogoutModal] = useState(false)

  const username = localStorage.getItem('username')

  const hasActiveFilters = searchName.trim() || minPrice || maxPrice || sortBy

  const resetImageUpload = () => {
    // Revoke object URL to prevent memory leak
    if (imagePreviewUrl && imagePreviewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(imagePreviewUrl)
    }
    setSelectedImageFile(null)
    setSelectedImageName('')
    setImagePreviewUrl('')
    setImageError('')
  }

  const closeFormModal = () => {
    setFormMode(null)
    setFormError('')
    setFieldErrors({})
    setProductForm(emptyForm)
    resetImageUpload()
  }

  const fetchProducts = async (
    targetPage = page,
    filters = {
      name: searchName,
      minPrice,
      maxPrice,
      sortBy,
      sortOrder,
    },
  ) => {
    setLoading(true)
    setError('')
    setFilterError('')

    try {
      const response = await api.get('/products', {
        params: {
          page: targetPage,
          pageSize,
          name: filters.name.trim() || undefined,
          minPrice: filters.minPrice === '' ? undefined : Number(filters.minPrice),
          maxPrice: filters.maxPrice === '' ? undefined : Number(filters.maxPrice),
          sortBy: filters.sortBy || undefined,
          sortOrder: filters.sortOrder || undefined,
        },
      })

      const result = response.data.data
      setProducts(result.data ?? [])
      setPage(result.page ?? targetPage)
      setTotalPages(result.totalPages ?? 1)
      setTotalProducts(result.total ?? 0)
    } catch (requestError) {
      const message = requestError.response?.data?.message || ''
      if (message.includes('minPrice')) {
        setFilterError('Min price tidak boleh lebih besar dari max price')
      }
      setError(
        message ||
          'Unable to load products. Please check your connection and try again.',
      )
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    const token = localStorage.getItem('token')

    if (!token) {
      navigate('/login', { replace: true })
    }
  }, [navigate])

  useEffect(() => {
    fetchProducts(page)
  }, [page])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      if (page === 1) {
        fetchProducts(1)
      } else {
        setPage(1)
      }
    }, 500)

    return () => window.clearTimeout(timeoutId)
  }, [searchName])

  const handleLogout = () => {
    localStorage.clear()
    navigate('/login')
  }

  const handleFormChange = (event) => {
    const { name, value } = event.target

    if (name === 'price') {
      const formatted = formatPriceInput(value)
      setProductForm((current) => ({ ...current, [name]: formatted }))
    } else {
      setProductForm((current) => ({ ...current, [name]: value }))
    }
  }

  const handleOpenAddModal = () => {
    setProductForm(emptyForm)
    setFormError('')
    setFieldErrors({})
    resetImageUpload()
    setFormMode('add')
  }

  const handleOpenEditModal = (product) => {
    setProductForm({
      id: product.id,
      name: product.name,
      description: product.description || '',
      price: formatPriceInput(String(product.price)),
    })
    setSelectedImageFile(null)
    setSelectedImageName('')
    setImagePreviewUrl(product.imageUrl || '')
    setImageError('')
    setFormError('')
    setFieldErrors({})
    setFormMode('edit')
  }

  const handleImageChange = (event) => {
    const file = event.target.files?.[0]

    if (!file) {
      resetImageUpload()
      return
    }

    if (!allowedImageTypes.includes(file.type)) {
      // Clear preview URL on validation failure
      if (imagePreviewUrl && imagePreviewUrl.startsWith('blob:')) {
        URL.revokeObjectURL(imagePreviewUrl)
      }
      setSelectedImageFile(null)
      setSelectedImageName('')
      setImagePreviewUrl('')
      setImageError('Please select a JPG, PNG, or WEBP image.')
      return
    }

    if (file.size > maxImageSize) {
      // Clear preview URL on validation failure
      if (imagePreviewUrl && imagePreviewUrl.startsWith('blob:')) {
        URL.revokeObjectURL(imagePreviewUrl)
      }
      setSelectedImageFile(null)
      setSelectedImageName('')
      setImagePreviewUrl('')
      setImageError('Image must be 2MB or smaller.')
      return
    }

    // Revoke old object URL before creating new one to prevent memory leak
    if (imagePreviewUrl && imagePreviewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(imagePreviewUrl)
    }

    setSelectedImageFile(file)
    setSelectedImageName(file.name)
    setImagePreviewUrl(URL.createObjectURL(file))
    setImageError('')
  }

  const handleApplyFilter = () => {
    if (page === 1) {
      fetchProducts(1)
    } else {
      setPage(1)
    }
  }

  const handleSortChange = (event) => {
    const value = event.target.value

    if (!value) {
      setSortBy('')
      setSortOrder('')
    } else {
      const [nextSortBy, nextSortOrder] = value.split(':')
      setSortBy(nextSortBy)
      setSortOrder(nextSortOrder)
    }

    if (page === 1) {
      fetchProducts(1, {
        name: searchName,
        minPrice,
        maxPrice,
        sortBy: value ? value.split(':')[0] : '',
        sortOrder: value ? value.split(':')[1] : '',
      })
    } else {
      setPage(1)
    }
  }

  const handleResetFilters = () => {
    setSearchName('')
    setMinPrice('')
    setMaxPrice('')
    setSortBy('')
    setSortOrder('')
    setFilterError('')

    if (page === 1) {
      fetchProducts(1, {
        name: '',
        minPrice: '',
        maxPrice: '',
        sortBy: '',
        sortOrder: '',
      })
    } else {
      setPage(1)
    }
  }

  const handleSubmitProduct = async (event) => {
    event.preventDefault()

    const errors = {}
    if (!productForm.name.trim()) errors.name = true
    if (!productForm.price || productForm.price === '0') errors.price = true
    
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    if (imageError) {
      return
    }

    setIsSubmitting(true)
    setFormError('')

    try {
      const rawPrice = parseFormattedPrice(productForm.price)
      const payload = {
        name: productForm.name.trim(),
        description: productForm.description.trim() || null,
        price: Number(rawPrice),
      }

      let productId = productForm.id

      if (formMode === 'edit') {
        const response = await api.put(`/products/${productForm.id}`, payload)
        productId = response.data.data?.id ?? productForm.id
      } else {
        const response = await api.post('/products', payload)
        productId = response.data.data?.id
      }

      if (selectedImageFile && productId) {
        const formData = new FormData()
        formData.append('file', selectedImageFile)
        await api.post(`/products/${productId}/image`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        })
      }

      if (page === 1) {
        await fetchProducts(1)
      } else {
        setPage(1)
      }

      closeFormModal()
    } catch (requestError) {
      const data = requestError.response?.data
      const message =
        data?.message ||
        data?.errors?.Price?.[0] ||
        data?.errors?.Name?.[0] ||
        data?.errors?.Description?.[0] ||
        `Unable to ${formMode === 'edit' ? 'update' : 'create'} product. Please try again.`
      setFormError(message)
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleConfirmDelete = async () => {
    if (!deleteTarget) {
      return
    }

    setIsSubmitting(true)
    setDeleteError('')

    try {
      await api.delete(`/products/${deleteTarget.id}`)
      await fetchProducts()
      setDeleteTarget(null)
    } catch (requestError) {
      setDeleteError(
        requestError.response?.data?.message ||
        'Unable to delete product. Please try again.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      {/* Premium Glass Navbar */}
      <nav className="navbar">
        <div className="container">
          <div className="d-flex align-items-center justify-content-between w-100">
            <div className="d-flex align-items-center gap-3">
              <div
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: '40px',
                  height: '40px',
                  background: 'var(--color-primary)',
                  borderRadius: '12px',
                }}
              >
                <svg
                  width="20"
                  height="20"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="white"
                  strokeWidth="2.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
                  <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                  <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                </svg>
              </div>
              <span className="navbar-brand" style={{ margin: 0 }}>Vault</span>
            </div>
            <div className="d-flex align-items-center gap-3">
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <div
                  style={{
                    width: '8px',
                    height: '8px',
                    background: '#10B981',
                    borderRadius: '50%',
                    boxShadow: '0 0 8px rgba(16, 185, 129, 0.6)',
                  }}
                />
                <span style={{ color: 'var(--color-foreground)', fontSize: '14px', fontWeight: '500' }}>
                  {username || 'Guest'}
                </span>
              </div>
              <button
                type="button"
                className="btn btn-outline-secondary btn-sm"
                onClick={() => setShowLogoutModal(true)}
                disabled={isSubmitting}
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="container" style={{ flex: 1, paddingTop: '32px', paddingBottom: '32px' }}>
        {/* Error Banner */}
        {error && !error.includes('minPrice') ? (
          <div
            style={{
              marginBottom: '24px',
              padding: '16px 20px',
              borderRadius: '14px',
              background: 'rgba(220, 38, 38, 0.1)',
              border: '1px solid rgba(220, 38, 38, 0.3)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'flex-start', gap: '12px' }}>
              <svg
                width="20"
                height="20"
                viewBox="0 0 24 24"
                fill="none"
                stroke="#DC2626"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                style={{ flexShrink: 0, marginTop: '2px' }}
              >
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <div style={{ flex: 1 }}>
                <div style={{ color: '#DC2626', fontSize: '14px', fontWeight: '600', marginBottom: '4px' }}>
                  Error loading products
                </div>
                <div style={{ color: '#DC2626', fontSize: '13px', marginBottom: '12px', opacity: 0.9 }}>
                  {error}
                </div>
                <button
                  type="button"
                  className="btn btn-sm"
                  onClick={() => fetchProducts()}
                  style={{
                    background: '#DC2626',
                    color: 'white',
                    border: 'none',
                    padding: '6px 16px',
                    borderRadius: '8px',
                    fontSize: '13px',
                    fontWeight: '500',
                  }}
                >
                  Retry
                </button>
              </div>
            </div>
          </div>
        ) : null}

        {/* Filters Section */}
        <div
          className="card"
          style={{
            marginBottom: '32px',
            padding: '8px',
          }}
        >
          <div className="row g-2 align-items-end">
            <div className="col-12 col-md">
              <label className="form-label" style={{ fontSize: '13px' }}>
                Search Products
              </label>
              <input
                type="text"
                className="form-control"
                placeholder="Search by product name..."
                value={searchName}
                onChange={(event) => setSearchName(event.target.value)}
                disabled={isSubmitting}
              />
            </div>
            <div className="col-6 col-md-2">
              <label className="form-label" style={{ fontSize: '13px' }}>
                Min Price
              </label>
              <input
                type="number"
                className="form-control"
                placeholder="100000"
                min="0"
                value={minPrice}
                onChange={(event) => setMinPrice(event.target.value)}
                disabled={isSubmitting}
              />
            </div>
            <div className="col-6 col-md-2">
              <label className="form-label" style={{ fontSize: '13px' }}>
                Max Price
              </label>
              <input
                type="number"
                className="form-control"
                placeholder="10000000"
                min="0"
                value={maxPrice}
                onChange={(event) => setMaxPrice(event.target.value)}
                disabled={isSubmitting}
              />
            </div>
            <div className="col-12 col-md-2">
              <label className="form-label" style={{ fontSize: '13px' }}>
                Sort By
              </label>
              <select
                className="form-select"
                value={sortBy && sortOrder ? `${sortBy}:${sortOrder}` : ''}
                onChange={handleSortChange}
                disabled={isSubmitting}
              >
                <option value="">Default</option>
                <option value="name:asc">Name (A-Z)</option>
                <option value="name:desc">Name (Z-A)</option>
                <option value="price:asc">Price (Low-High)</option>
                <option value="price:desc">Price (High-Low)</option>
              </select>
            </div>
            <div className="col-6 col-md-auto">
              <button
                type="button"
                className="btn btn-outline-secondary w-100"
                onClick={handleApplyFilter}
                disabled={isSubmitting}
              >
                Apply
              </button>
            </div>
            <div className="col-6 col-md-auto">
              <button
                type="button"
                className="btn btn-outline-dark w-100"
                onClick={handleResetFilters}
                disabled={isSubmitting}
              >
                Clear
              </button>
            </div>
            <div className="col-12 col-md-auto ms-md-auto">
              <button
                type="button"
                className="btn btn-primary w-100"
                onClick={handleOpenAddModal}
                disabled={isSubmitting}
              >
                <span style={{ display: 'flex', alignItems: 'center', gap: '6px', justifyContent: 'center' }}>
                  <svg
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <line x1="12" y1="5" x2="12" y2="19" />
                    <line x1="5" y1="12" x2="19" y2="12" />
                  </svg>
                  Add Product
                </span>
              </button>
            </div>
          </div>
          {filterError ? (
            <div
              style={{
                color: '#DC2626',
                fontSize: '12px',
                marginTop: '10px',
                paddingLeft: '4px',
              }}
            >
              {filterError}
            </div>
          ) : null}
        </div>

        {/* Products Table */}
        <div className="card" style={{ overflow: 'hidden' }}>
          <div className="table-responsive">
            <table className="table align-middle mb-0">
              <thead>
                <tr>
                  <th>Image</th>
                  <th>Name</th>
                  <th>Description</th>
                  <th>Price</th>
                  <th>Created At</th>
                  <th className="text-end">Actions</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan="6" className="text-center py-4">
                      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px', padding: '48px 0' }}>
                        <div
                          style={{
                            width: '40px',
                            height: '40px',
                            border: '4px solid rgba(220, 38, 38, 0.15)',
                            borderTopColor: '#DC2626',
                            borderRadius: '50%',
                            animation: 'spin 0.8s linear infinite',
                          }}
                        />
                        <span style={{ color: 'var(--color-muted)', fontSize: '14px' }}>
                          Loading products...
                        </span>
                      </div>
                    </td>
                  </tr>
                ) : products.length === 0 ? (
                  <tr>
                    <td colSpan="6" className="text-center py-5">
                      {/* Empty state: no products at all vs no search results */}
                      {!hasActiveFilters && totalProducts === 0 ? (
                        <div style={{ padding: '48px 24px' }}>
                          <div
                            style={{
                              width: '72px',
                              height: '72px',
                              margin: '0 auto 20px',
                              background: 'rgba(255, 255, 255, 0.05)',
                              borderRadius: '20px',
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                              border: '1px solid rgba(255, 255, 255, 0.08)',
                            }}
                          >
                            <svg
                              width="36"
                              height="36"
                              viewBox="0 0 24 24"
                              fill="none"
                              stroke="var(--color-muted)"
                              strokeWidth="1.5"
                              strokeLinecap="round"
                              strokeLinejoin="round"
                            >
                              <path d="M20 7h-9" />
                              <path d="M14 17H5" />
                              <circle cx="17" cy="17" r="3" />
                              <circle cx="7" cy="7" r="3" />
                            </svg>
                          </div>
                          <h3 style={{ color: 'var(--color-foreground)', fontSize: '18px', fontWeight: '600', marginBottom: '8px' }}>
                            No products yet
                          </h3>
                          <p style={{ color: 'var(--color-muted)', fontSize: '14px', marginBottom: '20px' }}>
                            Get started by adding your first product to the inventory
                          </p>
                          <button
                            type="button"
                            className="btn btn-primary"
                            onClick={handleOpenAddModal}
                            disabled={isSubmitting}
                          >
                            <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                              <svg
                                width="16"
                                height="16"
                                viewBox="0 0 24 24"
                                fill="none"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                              >
                                <line x1="12" y1="5" x2="12" y2="19" />
                                <line x1="5" y1="12" x2="19" y2="12" />
                              </svg>
                              Add Your First Product
                            </span>
                          </button>
                        </div>
                      ) : (
                        <div style={{ padding: '48px 24px' }}>
                          <div
                            style={{
                              width: '72px',
                              height: '72px',
                              margin: '0 auto 20px',
                              background: 'rgba(255, 255, 255, 0.05)',
                              borderRadius: '20px',
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                              border: '1px solid rgba(255, 255, 255, 0.08)',
                            }}
                          >
                            <svg
                              width="36"
                              height="36"
                              viewBox="0 0 24 24"
                              fill="none"
                              stroke="var(--color-muted)"
                              strokeWidth="1.5"
                              strokeLinecap="round"
                              strokeLinejoin="round"
                            >
                              <circle cx="11" cy="11" r="8" />
                              <line x1="21" y1="21" x2="16.65" y2="16.65" />
                            </svg>
                          </div>
                          <h3 style={{ color: 'var(--color-foreground)', fontSize: '18px', fontWeight: '600', marginBottom: '8px' }}>
                            No products match your search
                          </h3>
                          <p style={{ color: 'var(--color-muted)', fontSize: '14px', marginBottom: '20px' }}>
                            Try adjusting your filters or search terms
                          </p>
                          <button
                            type="button"
                            className="btn btn-outline-secondary"
                            onClick={handleResetFilters}
                            disabled={isSubmitting}
                          >
                            Clear Filters
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ) : (
                  products.map((product) => (
                    <tr key={product.id}>
                      <td>
                        <ProductImage imageUrl={product.imageUrl} alt={product.name} />
                      </td>
                      <td style={{ fontWeight: '500' }}>{product.name}</td>
                      <td style={{ color: 'var(--color-muted)', fontSize: '13px' }}>
                        {product.description || '-'}
                      </td>
                      <td style={{ fontWeight: '600', color: 'var(--color-accent)' }}>
                        {formatPrice(product.price)}
                      </td>
                      <td style={{ fontSize: '13px', color: 'var(--color-muted)' }}>
                        {formatDate(product.createdAt)}
                      </td>
                      <td className="text-end">
                        <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                          <button
                            type="button"
                            className="btn btn-outline-primary btn-sm"
                            onClick={() => handleOpenEditModal(product)}
                            disabled={isSubmitting}
                          >
                            Edit
                          </button>
                          <button
                            type="button"
                            className="btn btn-outline-danger btn-sm"
                            onClick={() => setDeleteTarget(product)}
                            disabled={isSubmitting}
                          >
                            Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Pagination */}
        {!loading && products.length > 0 ? (
          <div className="d-flex justify-content-between align-items-center mt-3">
            <button
              type="button"
              className="btn btn-outline-secondary"
              onClick={() => setPage((current) => Math.max(1, current - 1))}
              disabled={page <= 1 || loading || isSubmitting}
            >
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                style={{ marginRight: '4px' }}
              >
                <polyline points="15 18 9 12 15 6" />
              </svg>
              Previous
            </button>
            <span style={{ color: 'var(--color-muted)', fontSize: '14px', fontWeight: '500' }}>
              Page {page} of {totalPages}
            </span>
            <button
              type="button"
              className="btn btn-outline-secondary"
              onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
              disabled={page >= totalPages || loading || isSubmitting}
            >
              Next
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                style={{ marginLeft: '4px' }}
              >
                <polyline points="9 18 15 12 9 6" />
              </svg>
            </button>
          </div>
        ) : null}
      </main>

      {formMode ? (
        <ProductFormModal
          fieldErrors={fieldErrors}
          form={productForm}
          formError={formError}
          imageError={imageError}
          imagePreviewUrl={imagePreviewUrl}
          isSubmitting={isSubmitting}
          mode={formMode}
          onChange={handleFormChange}
          onClose={closeFormModal}
          onImageChange={handleImageChange}
          onSubmit={handleSubmitProduct}
          selectedImageName={selectedImageName}
        />
      ) : null}

      {deleteTarget ? (
        <ConfirmModal
          deleteError={deleteError}
          isSubmitting={isSubmitting}
          title="Delete Product"
          message={`Delete "${deleteTarget.name}"? This action cannot be undone.`}
          onClose={() => {
            setDeleteTarget(null)
            setDeleteError('')
          }}
          onConfirm={handleConfirmDelete}
        />
      ) : null}

      {showLogoutModal ? (
        <LogoutConfirmModal
          isSubmitting={isSubmitting}
          onClose={() => setShowLogoutModal(false)}
          onConfirm={() => {
            setShowLogoutModal(false)
            handleLogout()
          }}
        />
      ) : null}

      <style>{`
        @keyframes spin {
          to { transform: rotate(360deg); }
        }
      `}</style>
    </div>
  )
}

export default Products
