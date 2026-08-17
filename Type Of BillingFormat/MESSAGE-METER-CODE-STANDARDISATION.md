Ms Jean, we found that you create a new meter for every customer — for example,
when each customer orders 2 machines.

For example:

For 3 customers, you would create the meter and item code as:

```
01. RA-3 UNIT
01. RA - 3 UNIT
01. RA- 3 UNIT
```

The only difference is the spacing.

Our proposed solution is to standardize one meter, "RA-3 UNIT", and remove the
"01" in front. The Item Description on the invoice will then be created
automatically by the system, and will not follow the meter description / item
description that you preset in the Item module / Meter module.

For example: your contract debtor is ABD SDN BHD, with 3 machines of model
IRADVDX6855I, and it is the first month of rental.

So the system will generate:

```
ABD SDN BHD
MODEL:IRADVDX6855I 3 UNIT
MONTHLY RENTAL (1/36)
```
