FROM quay.io/pypa/manylinux_2_28_x86_64 AS build
RUN dnf install clang-devel -y
COPY wren wren/
RUN export PATH=/opt/python/cp37-cp37m/bin:\$PATH && /usr/bin/make -C wren vm

FROM build AS lib
COPY --from=build wren/lib wren/lib
COPY . .
