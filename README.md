# FastSerializer
IL-based binary serializer

This is the initial serializer I wrote and intended to use for VFW Fast.Save. It's an IL-based serializer, emits IL to genreate the necessary serialization functions. 

Supports inheritance, cyclic refernces, serialization by reference, single dimension and 2d arrays, generic Lists, Queues, Stacks, HashSet, Dictionary, Metadata (Type, FieldInfo, PropertyInfo, MethodInfo), enms, Guid, Nullables, structs/classes and some Unity types. 

You can either emit the code to memory (CompileDynamic) or to a DLL (for use in AOT platforms)

This was the last iteration on the serializer before I ditched it and wrote and used BinaryX 2.0 instead. This is the 'dynamic' version, which means you don't have to feed it the types you want to serialize in advance, it can generate the serialization code necessary for any type it is not familiar with/comes acorss the first time which makes it super powerful and convenient to use.

See the test project for usage examples.

I ditched this serializer because it was hard to debug and develop. I put it here for employers to see my work and in case anyone's interested in this stuff and wants to give it a go with his own IL-based serializer. (Advice: know in advance what you're doing, if you never wrote a serializer before, an IL-based one is probably not a good one to start with. Start with something simple: reflective)
