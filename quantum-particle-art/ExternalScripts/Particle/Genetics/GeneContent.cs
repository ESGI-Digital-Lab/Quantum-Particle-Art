namespace UnityEngine.ExternalScripts.Particle.Genetics;

public struct GeneContent
{
    private byte _typeId;
    private byte _input;

    public GeneContent(byte typeId, byte input)
    {
        _typeId = typeId;
        _input = input;
    }

    public byte TypeId => _typeId;

    public byte Input => _input;

    public override string ToString()
    {
        return $"{_typeId}({_input})";
    }
}