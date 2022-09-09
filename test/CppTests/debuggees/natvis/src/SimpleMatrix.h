class SimpleMatrix
{
public:
    int m_size1;
    int m_size2;
    bool m_fUseSize1;
    int* m_pData;

    SimpleMatrix(int size1, int size2, bool fUseSize1)
    {
        m_size1 = size1;
        m_size2 = size2;
        m_fUseSize1 = fUseSize1;

        m_pData = new int[GetSize()];

        for (int i = 0; i < GetSize(); i++)
        {
            m_pData[i] = i;
        }
    }

    int GetSize()
    {
        return m_fUseSize1 ? m_size1 : m_size2;
    }
};