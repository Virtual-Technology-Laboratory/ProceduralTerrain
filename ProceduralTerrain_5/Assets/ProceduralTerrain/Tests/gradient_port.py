import numpy as np

np.random.seed(424)
x = np.random.rand(4,3)

dx, dy = np.gradient(x)
print dx
print dy

def gradient(vals):
    nx, ny = vals.shape
    dx = np.zeros(vals.shape)
    dy = np.zeros(vals.shape)

    for i in xrange(1, nx-1):
        for j in xrange(ny):
            dx[i, j] = (vals[i + 1, j] - vals[i - 1, j]) / 2.0
            
    for i in xrange(nx):
        dy[i, 0] = vals[i, 1] - vals[i, 0]
        dy[i, ny-1] = vals[i, ny-1] - vals[i, ny-2]

    for j in xrange(ny):
        dx[0, j] = vals[1, j] - vals[0, j]
        dx[nx-1, j] = vals[nx-1, j] - vals[nx-2, j]
        
    for i in xrange(nx):
        for j in xrange(1, ny-1):
            dy[i,j] = (vals[i, j + 1] - vals[i, j - 1]) / 2.0
            
    return dx, dy

dx, dy = gradient(x)
print dx
print dy
