import './App.css'

import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Navbar } from './components/Navbar';
import { Test } from './pages/Test';
import { Main } from './pages/Main';
import { Billing } from './pages/Billing';
import { Login } from './pages/Login';
import { ProtectedRoute } from './components/ProtectedRoute';

function App() {

  return (
    <Router>
      <Routes>

        {/* login/regist page - in case of user not logged */}
        <Route path="/login" element={<Login />} />

        {/* main pages - in case of user logged */}
        <Route
          path="/*"
          element={
            <ProtectedRoute>
              <>
                <Navbar />
                <Routes>
                  <Route path="/" element={<Main />} />
                  <Route path="/test" element={<Test />} />
                  <Route path="/bill" element={<Billing />} />
                </Routes>
              </>
            </ProtectedRoute>
          }
        />
      </Routes>
    </Router>
  )
}

export default App
