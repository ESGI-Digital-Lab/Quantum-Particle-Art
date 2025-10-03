namespace UnityEngine.ExternalScripts.Particle.Genetics;

public struct GeneContent
{
    private byte _typeId;

    public GeneContent(byte typeId)
    {
        _typeId = typeId;
    }

    public byte TypeId => _typeId;


    public override string ToString()
    {
        return $"{_typeId}";
    }
}