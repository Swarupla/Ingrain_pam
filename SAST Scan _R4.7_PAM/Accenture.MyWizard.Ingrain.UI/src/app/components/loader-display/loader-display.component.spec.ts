import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LoaderDisplayComponent } from './loader-display.component';

describe('LoaderDisplayComponent', () => {
  let component: LoaderDisplayComponent;
  let fixture: ComponentFixture<LoaderDisplayComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ LoaderDisplayComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LoaderDisplayComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
