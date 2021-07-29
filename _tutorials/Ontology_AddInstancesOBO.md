

# Minimal example to add instances to OBO ontology 

=> use `is_a` to link instances to a class

```
[Term]
id: NFDI4PSO:9999990
name: TEST_Dominik Class with instances
def: "TEST_Dominik"

[Term]
id: NFDI4PSO:9999991
name: Test instance 1
def: "TEST_Dominik instance"
is_a: NFDI4PSO:9999990

[Term]
id: NFDI4PSO:9999992
name: Test instance 2
def: "TEST_Dominik instance"
is_a: NFDI4PSO:9999990
```
