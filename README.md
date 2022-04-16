# Holoware

A Software City for visualizing both static and dynamic aspects of software.

Additional information about the layout of DynaCity can be found in the publication
_V. Dashuber, M. Philippsen, J. Weigend, A Layered Software City for Dependency Visualization, in: Proc. 16th Intl.
Joint Conf. on Computer Vision, Imaging and Computer Graphics Theory and Applications, Online, 2021, pp. 15–26_.

The trace visualization is published in _V. Dashuber, M. Philippsen, Trace Visualization within the Software City
Metaphor: A Controlled Experiment on Program Comprehension, in: Proc. IEEE Work. Conf. on Softw. Vis., Online, 2021, pp.
55–64_. The source code contains an extended version where the arcs and buildings are colored based on the HTTP
error code of the trace data.

### Requirements

* Unity
* C#

### Try it out

You can use the prepared `Assets/Scenes/Main Scene.unity` to try it out.

For trying the visualization with for own data, you need to generate a dependency graph of your application. The
required input format corresponds to
the [`jdeps`](https://docs.oracle.com/javase/8/docs/technotes/tools/unix/jdeps.html) analyses with a dot output, e.g.
under `Assets/Resources/Dot/spring-boot-realworld-example-app.dot`. The format for the spans can be seen under `
Assets/Resources/TraceData/`, it is the exported trace data from
the [Elastic stack](https://www.elastic.co/elastic-stack/). Other importers can be written too. 

