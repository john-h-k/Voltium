

struct Complex
{
    float Real, Imaginary;
};

struct ComplexD
{
    double Real, Imaginary;
};


Complex Complex_Create(float real, float imaginary)
{
    Complex complex;
    complex.Real = real;
    complex.Imaginary = imaginary;
    return complex;
}

float2 Complex_AsFloat2(Complex complex)
{
    return float2(complex.Real, complex.Imaginary);
}

Complex Complex_Create(float2 complex)
{
    return Complex_Create(complex.x, complex.y);
}

Complex Complex_Negate(Complex complex)
{
    return Complex_Create(-complex.Real, -complex.Imaginary);
}

Complex Complex_Add(Complex left, Complex right)
{
    return Complex_Create(left.Real + right.Real, left.Imaginary + right.Imaginary);
}

Complex Complex_Sub(Complex left, Complex right)
{
    return Complex_Create(left.Real - right.Real, left.Imaginary - right.Imaginary);
}

Complex Complex_Mul(Complex left, Complex right)
{
    float real = (left.Real * right.Real) - (left.Imaginary * right.Imaginary);
    float imaginary = (left.Imaginary * right.Real) + (left.Real * right.Imaginary);

    return Complex_Create(real, imaginary);
}

Complex Complex_Div(Complex left, Complex right)
{
    float real, imaginary;
    if (abs(right.Imaginary) < abs(right.Real))
    {
        float div = right.Imaginary / right.Real;
        real = (left.Real + left.Imaginary * div) / (right.Real + right.Imaginary * div);
        imaginary = (left.Imaginary - left.Real * div) / (right.Real + right.Imaginary * div);
    }
    else
    {
        float div = right.Real / right.Imaginary;
        real = (left.Imaginary + left.Real * div) / (right.Imaginary + right.Real * div);
        imaginary = (-left.Real + left.Imaginary * div) / (right.Imaginary + right.Real* div);
    }
    return Complex_Create(real, imaginary);
}

float Complex_MagnitudeSquared(Complex complex)
{
    return abs(complex.Real * complex.Real + complex.Imaginary * complex.Imaginary);
}
float Complex_Magnitude(Complex complex)
{
    return sqrt(Complex_MagnitudeSquared(complex));
}

ComplexD Complex_Create(double real, double imaginary)
{
    ComplexD ComplexD;
    ComplexD.Real = real;
    ComplexD.Imaginary = imaginary;
    return ComplexD;
}

ComplexD Complex_Create(double2 complex)
{
    return Complex_Create(complex.x, complex.y);
}

ComplexD Complex_Negate(ComplexD ComplexD)
{
    return Complex_Create(-ComplexD.Real, -ComplexD.Imaginary);
}

ComplexD Complex_Add(ComplexD left, ComplexD right)
{
    return Complex_Create(left.Real + right.Real, left.Imaginary + right.Imaginary);
}

ComplexD Complex_Sub(ComplexD left, ComplexD right)
{
    return Complex_Create(left.Real - right.Real, left.Imaginary - right.Imaginary);
}

ComplexD Complex_Mul(ComplexD left, ComplexD right)
{
    double real = (left.Real * right.Real) - (left.Imaginary * right.Imaginary);
    double imaginary = (left.Imaginary * right.Real) + (left.Real * right.Imaginary);

    return Complex_Create(real, imaginary);
}

ComplexD Complex_Div(ComplexD left, ComplexD right)
{
    double real, imaginary;
    if (abs(right.Imaginary) < abs(right.Real))
    {
        double div = right.Imaginary / right.Real;
        real = (left.Real + left.Imaginary * div) / (right.Real + right.Imaginary * div);
        imaginary = (left.Imaginary - left.Real * div) / (right.Real + right.Imaginary * div);
    }
    else
    {
        double div = right.Real / right.Imaginary;
        real = (left.Imaginary + left.Real * div) / (right.Imaginary + right.Real * div);
        imaginary = (-left.Real + left.Imaginary * div) / (right.Imaginary + right.Real * div);
    }
    return Complex_Create(real, imaginary);
}

double Complex_MagnitudeSquared(ComplexD complex)
{
    return abs(complex.Real * complex.Real + complex.Imaginary * complex.Imaginary);
}
float Complex_Magnitude(ComplexD complex)
{
    return sqrt((float) Complex_MagnitudeSquared(complex));
}
