

struct Complex
{
    float Real, Imaginary;
};

RWStructuredBuffer<Complex> c;


Complex Complex_Create(float real, float imaginary)
{
    Complex complex;
    complex.Real = real;
    complex.Imaginary = imaginary;
    return complex;
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
    float imaginary = (left.Imaginary * right.Real) + (left.Real + right.Imaginary);

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
