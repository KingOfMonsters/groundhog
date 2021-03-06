---
title:      Channels
excerpt:    Components for deriving and analysing water flows within a sectional profile.
date:       18-07-31
files:      true
files_text: model and definition that demonstrating the use of these components
thumbnail:  thumbnail.jpg
---

- Introduction to general hydraulic principles
- Describe process for deriving level from flow quantity; noting not that water does not strictly follow this process (i.e. settling effects)

{% include elements/component.html title='ChannelRegion' %}

- Description of the calculated attributes and their meaning/purpose
- More detailed discussion of manning formula and link to predefined values for channel materials (noting the uncertainty involved in using them)

{% include elements/component.html title='ChannelInfo' %}

- Discussion of applications to design

{% include elements/figure.html image='channels/model.jpg' alt='Images of the channel tool applied to various geometries' %}

{% include elements/figure.html image='channels/definition.jpg' caption='Grasshopper definition demonstrating how to use the channel region and channel profile components.' credit='Philip Belesky, for http://groundhog.la' %}
