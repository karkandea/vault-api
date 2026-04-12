import { Navigate, Route, Routes } from 'react-router-dom'
import Login from './pages/Login.jsx'
import Products from './pages/Products.jsx'
import Register from './pages/Register.jsx'

function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/products" replace />} />
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/products" element={<Products />} />
    </Routes>
  )
}

export default App
