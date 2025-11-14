# TensileTesting

## Software for reading and processing data from a tensile testing machine via industrial ADC modules using the DCON protocol.

---

## :memo: **Note:** This project is at an early stage of development.

---

Tensile Testing is a modern software application designed for data acquisition, visualization, and processing for tensile testing machines. The software enables accurate measurement, real-time analysis, and in-depth evaluation of material strength experiments by using specimen elongation in millimeters (mm) and applied load in kilonewtons (kN).

At the core of the application lies its data acquisition module. Tensile Testing establishes a direct connection to industrial tensile machines using industry-standard communication protocols such as RS-485 and DCON, ensuring reliable and rapid transfer of measurement data. The application interfaces with analog-to-digital converters (ADCs), such as the **ICP-CON M-7017**, to capture high-resolution sensor readings with 16-bit accuracy across 8 input channels. These modules can be remotely controlled through a set of commands known as the DCON protocol. Communication between the module and the host system occurs in ASCII format over a bi-directional RS-485 serial bus.

Data streaming and logging are essential functions of the software. Tensile Testing provides continuous acquisition and storage of test data, timestamped for reproducibility and traceability. All measurements are saved locally and can be exported in popular formats (CSV, Excel, JPEG, PNG, PDF), enabling integration with laboratory management systems and further analysis.

A key feature is the automatic plotting and visualization of test results. During a tensile test, the application builds dynamic real-time graphs—typically load versus elongation curves—which are fundamental for material characterization. Users can observe the entire test process: from initial loading through elastic and plastic deformation stages up to specimen fracture. The intuitive chart interface offers zoom, and export to PDF/image, Excel, and CSV formats.

Beyond visualization, Tensile Testing incorporates advanced calculation modules for core mechanical properties. The software automatically determines:

- Maximum load and breaking point (kN)
- Elongation at failure (mm)
- Stress $(\sigma\)$ and strain $\(\varepsilon\)$ based on user-supplied sample dimensions:
> $$\sigma = \frac{F}{A}$$
>
> $$\varepsilon = \frac{L - L_0}{L_0}$$
- Young’s modulus (E) from the initial slope of the graph
- Yield points (upper/lower) using standard recognition algorithms
- Energy (area under the curve), ductility, and other key material indicators

Tensile Testing offers a user-friendly interface, allowing users to calibrate sensors and configure experiment parameters directly within the application. Built-in error detection and real-time diagnostics ensure measurement reliability and enhance the overall user experience.

The software is suitable for academic, industrial, and research laboratory environments. Typical users include materials engineers, quality control specialists, researchers, educators, and students performing tensile strength assessments for metals, polymers, composites, textiles, and more.

For reporting, Tensile Testing can generate `.csv`, `.xls`, and other files, including all graphs, statistical results, and experiment metadata. Reports are suitable for regulatory compliance, publication, and data sharing.
