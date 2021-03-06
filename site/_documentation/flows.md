---
title:      Flows
excerpt:    Components for creating simple projections and analysis of surface water flows.
date:       17-10-14
files:      true
files_text: model and definition that demonstrating the use of these components
thumbnail:  thumbnail.jpg
---

{% include elements/figure.html image='flows/1.jpg' caption='Surface water flow paths across a littoral region' credit='Image via Philip Belesky for the "Processes and Processors" project (http://philipbelesky.com/projects/processes-and-processors/)' %}

The "flows" components create naïve projections or simulations of surface water flows and provide further means to analyse the results of this calculation. The key component — the `FlowPath` accepts a series of 'drop points' on a `Surface` or `Mesh` that become the starting locations of each hypothetical flow path. From there, each point samples the surface or mesh to determine its slope, which becomes a directing vector (i.e. one that points 'downhill'). Each point is then moved along this vector a pre-specified distance, forming a line. The end of this line part then becomes the starting point for the next direction; creating a recursive process where flow paths assemble themselves as `Polylines` that grow through this series of descending jumps.

This 'gradient descent' process repeats until a path crosses the edge of the form,  after a specified quantity of iterations, or until the algorithm determines that the path has reached a point without a viable further downhill path. This halting calculation aims to identify a 'basin' where water might collect and pool rather than continue to flow downhill. The component then produces as an output a series of `Polylines`, from which the beginnings, ends, and individual segments can be readily extracted. The process provides degrees of flexibility. By accepting any given set of `Points` (rather than enforcing a spatial grid or other formation) it offers the ability to work across a number of contexts, from situations where you may want to simulate a uniform distribution (say rain) or just a particular point-source of water.

The `FlowPath` component takes two forms a `SurfaceFlowPath` and a `MeshFlowPath` depending on the geometric type of the 'landscape' you want to test.

{% include elements/component.html title='SurfaceFlowPath' %}
{% include elements/component.html title='MeshFlowPath' %}

Once calculated, these flow paths can then be used to support further analysis of the landscape's hydrological features.

The first component for this is `FlowCatchment`. It uses the collection of flow paths (knowing their end points) to identify different catchment areas. That is to say, it classifies each flow path into groups depending upon which paths finish or 'drain' into the same approximate location. This grouping is visually represented using a Voronoi diagram with each cell centred on the original `Pts` used as the 'start' of each path (adjacent cells of the same group will be merged). Additionally, the different catchment groups are provided with a distinct colour code and its cells/paths are output as distinct branches so they can be visualised or analysed further.

{% include elements/component.html title='FlowCatchment' %}

{% include elements/figure.html image='flows/model.jpg' alt='Image of the flows component used across two hypothetical landforms' %}

{% include elements/figure.html image='flows/definition.jpg' caption='Grasshopper definition demonstrating how to use the flow and catchment analysis for Surface and Mesh form.' credit='Philip Belesky, for http://groundhog.la' %}
