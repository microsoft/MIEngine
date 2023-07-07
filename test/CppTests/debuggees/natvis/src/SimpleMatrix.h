class SimpleMatrix
{
public:
    int m_rows;
    int m_cols;
    int* m_pData;

    SimpleMatrix(int row, int col)
    {
        m_rows = row;
        m_cols = col;

        m_pData = new int[row * col];

        for (int i = 0; i < row * col; i++)
        {
            m_pData[i] = i;
        }
    }
};