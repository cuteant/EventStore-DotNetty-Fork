# System.UriTemplate for .Net Standard

UriTemplate isn't currently available for .Net Standard, so this repo contains a copy of the .Net 4.7.2 reference source built as a .Net Standard library.

## Limitations

- Exception messages don't contain full messages, just the property name that's used to look up the message
- The additional tracing / debugging info the original provides isn't present
- Original library uses current HTTP request to determine the host in certain circumstances; this version makes no attempt to do this

## More on the limitations

The reference source uses a hidden `SR` class to lookup any strings for exception messages; I just made these all be the same as the property name used to look up a string, they're sufficiently usable for my purposes.

Assertions and exception are done via wrappers which perform additional work to aid debugging; I skipped most of this and used a thin wrapper that basically does nothing extra.

The original can be used as part of a HTTP request, in which case it uses the host from the request at times; adding this in is relatively complicated, and not something I need, so this version doesn't try to do this.
