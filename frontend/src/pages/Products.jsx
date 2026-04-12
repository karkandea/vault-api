import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/axios.js'

const pageSize = 10
const emptyForm = {
  id: null,
  name: '',
  description: '',
  price: '',
}

function formatPrice(price) {
  return new Intl.NumberFormat('id-ID', {
    style: 'currency',
    currency: 'IDR',
    maximumFractionDigits: 0,
  }).format(price)
}

function formatDate(value) {
  if (!value) {
    return '-'
  }

  return new Date(value).toLocaleString()
}

function ProductFormModal({
  form,
  isSubmitting,
  mode,
  onChange,
  onClose,
  onSubmit,
}) {
  const title = mode === 'edit' ? 'Edit Product' : 'Add Product'
  const submitLabel = isSubmitting
    ? mode === 'edit'
      ? 'Saving...'
      : 'Creating...'
    : mode === 'edit'
      ? 'Save Changes'
      : 'Create Product'

  return (
    <>
      <div
        className="modal d-block"
        tabIndex="-1"
        role="dialog"
        aria-modal="true"
        aria-labelledby="product-form-modal-title"
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
                <div className="mb-3">
                  <label htmlFor="product-name" className="form-label">
                    Name
                  </label>
                  <input
                    id="product-name"
                    name="name"
                    type="text"
                    className="form-control"
                    value={form.name}
                    onChange={onChange}
                    required
                  />
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
                    value={form.description}
                    onChange={onChange}
                  />
                </div>

                <div>
                  <label htmlFor="product-price" className="form-label">
                    Price
                  </label>
                  <input
                    id="product-price"
                    name="price"
                    type="number"
                    className="form-control"
                    min="0"
                    step="1"
                    value={form.price}
                    onChange={onChange}
                    required
                  />
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

function ConfirmModal({ isSubmitting, message, onClose, onConfirm, title }) {
  return (
    <>
      <div
        className="modal d-block"
        tabIndex="-1"
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-modal-title"
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

function FeedbackModal({ message, onClose, title }) {
  return (
    <>
      <div
        className="modal d-block"
        tabIndex="-1"
        role="dialog"
        aria-modal="true"
        aria-labelledby="feedback-modal-title"
      >
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <div className="modal-header">
              <h2 className="modal-title fs-5" id="feedback-modal-title">
                {title}
              </h2>
              <button
                type="button"
                className="btn-close"
                aria-label="Close"
                onClick={onClose}
              />
            </div>

            <div className="modal-body">
              <p className="mb-0">{message}</p>
            </div>

            <div className="modal-footer">
              <button type="button" className="btn btn-primary" onClick={onClose}>
                OK
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
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [searchName, setSearchName] = useState('')
  const [minPrice, setMinPrice] = useState('')
  const [maxPrice, setMaxPrice] = useState('')
  const [sortBy, setSortBy] = useState('')
  const [sortOrder, setSortOrder] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [formMode, setFormMode] = useState(null)
  const [productForm, setProductForm] = useState(emptyForm)
  const [deleteTarget, setDeleteTarget] = useState(null)
  const [feedbackMessage, setFeedbackMessage] = useState('')

  const username = localStorage.getItem('username')

  const closeFormModal = () => {
    setFormMode(null)
    setProductForm(emptyForm)
  }

  const openFeedbackModal = (message) => {
    setFeedbackMessage(message)
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
    } catch (requestError) {
      setError(
        requestError.response?.data?.message ||
          'Unable to load products. Please try again.',
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
    setProductForm((current) => ({ ...current, [name]: value }))
  }

  const handleOpenAddModal = () => {
    setProductForm(emptyForm)
    setFormMode('add')
  }

  const handleOpenEditModal = (product) => {
    setProductForm({
      id: product.id,
      name: product.name,
      description: product.description || '',
      price: String(product.price),
    })
    setFormMode('edit')
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
    setIsSubmitting(true)

    try {
      const payload = {
        name: productForm.name.trim(),
        description: productForm.description.trim() || null,
        price: Number(productForm.price),
      }

      if (formMode === 'edit') {
        await api.put(`/products/${productForm.id}`, payload)
        await fetchProducts()
      } else {
        await api.post('/products', payload)
        await fetchProducts(1)
      }

      closeFormModal()
    } catch (requestError) {
      openFeedbackModal(
        requestError.response?.data?.message ||
          `Unable to ${formMode === 'edit' ? 'update' : 'add'} product.`,
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleConfirmDelete = async () => {
    if (!deleteTarget) {
      return
    }

    setIsSubmitting(true)

    try {
      await api.delete(`/products/${deleteTarget.id}`)
      await fetchProducts()
      setDeleteTarget(null)
    } catch (requestError) {
      openFeedbackModal(
        requestError.response?.data?.message || 'Unable to delete product.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="min-vh-100 bg-light">
      <nav className="navbar navbar-expand-lg bg-white border-bottom">
        <div className="container">
          <span className="navbar-brand fw-semibold">Vault Products</span>
          <div className="d-flex align-items-center gap-3">
            <span className="text-muted">{username || 'Guest'}</span>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              onClick={handleLogout}
            >
              Logout
            </button>
          </div>
        </div>
      </nav>

      <main className="container py-4">
        <div className="row g-2 align-items-end mb-4">
          <div className="col-12 col-md">
            <input
              type="text"
              className="form-control"
              placeholder="Search by product name..."
              value={searchName}
              onChange={(event) => setSearchName(event.target.value)}
            />
          </div>
          <div className="col-6 col-md-2">
            <input
              type="number"
              className="form-control"
              placeholder="Min price"
              min="0"
              value={minPrice}
              onChange={(event) => setMinPrice(event.target.value)}
            />
          </div>
          <div className="col-6 col-md-2">
            <input
              type="number"
              className="form-control"
              placeholder="Max price"
              min="0"
              value={maxPrice}
              onChange={(event) => setMaxPrice(event.target.value)}
            />
          </div>
          <div className="col-6 col-md-2">
            <select
              className="form-select"
              value={sortBy && sortOrder ? `${sortBy}:${sortOrder}` : ''}
              onChange={handleSortChange}
            >
              <option value="">Sort By</option>
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
            >
              Apply Filter
            </button>
          </div>
          <div className="col-6 col-md-auto">
            <button
              type="button"
              className="btn btn-outline-dark w-100"
              onClick={handleResetFilters}
            >
              Clear
            </button>
          </div>
          <div className="col-6 col-md-auto ms-md-auto">
            <button
              type="button"
              className="btn btn-primary w-100"
              onClick={handleOpenAddModal}
            >
              Add Product
            </button>
          </div>
        </div>

        {error ? <div className="alert alert-danger">{error}</div> : null}

        <div className="card shadow-sm">
          <div className="table-responsive">
            <table className="table table-striped align-middle mb-0">
              <thead className="table-light">
                <tr>
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
                    <td colSpan="5" className="text-center py-4">
                      Loading products...
                    </td>
                  </tr>
                ) : products.length === 0 ? (
                  <tr>
                    <td colSpan="5" className="text-center py-4">
                      No products found.
                    </td>
                  </tr>
                ) : (
                  products.map((product) => (
                    <tr key={product.id}>
                      <td>{product.name}</td>
                      <td>{product.description || '-'}</td>
                      <td>{formatPrice(product.price)}</td>
                      <td>{formatDate(product.createdAt)}</td>
                      <td className="text-end">
                        <div className="btn-group btn-group-sm">
                          <button
                            type="button"
                            className="btn btn-outline-primary"
                            onClick={() => handleOpenEditModal(product)}
                          >
                            Edit
                          </button>
                          <button
                            type="button"
                            className="btn btn-outline-danger"
                            onClick={() => setDeleteTarget(product)}
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

        <div className="d-flex justify-content-between align-items-center mt-3">
          <button
            type="button"
            className="btn btn-outline-secondary"
            onClick={() => setPage((current) => Math.max(1, current - 1))}
            disabled={page <= 1 || loading}
          >
            Prev
          </button>
          <span className="text-muted">
            Page {page} of {totalPages}
          </span>
          <button
            type="button"
            className="btn btn-outline-secondary"
            onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
            disabled={page >= totalPages || loading}
          >
            Next
          </button>
        </div>
      </main>

      {formMode ? (
        <ProductFormModal
          form={productForm}
          isSubmitting={isSubmitting}
          mode={formMode}
          onChange={handleFormChange}
          onClose={closeFormModal}
          onSubmit={handleSubmitProduct}
        />
      ) : null}

      {deleteTarget ? (
        <ConfirmModal
          isSubmitting={isSubmitting}
          title="Delete Product"
          message={`Delete "${deleteTarget.name}"? This action cannot be undone.`}
          onClose={() => setDeleteTarget(null)}
          onConfirm={handleConfirmDelete}
        />
      ) : null}

      {feedbackMessage ? (
        <FeedbackModal
          title="Action Failed"
          message={feedbackMessage}
          onClose={() => setFeedbackMessage('')}
        />
      ) : null}
    </div>
  )
}

export default Products
