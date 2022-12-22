import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FiltersDataComponent } from './filters-data.component';

describe('FiltersDataComponent', () => {
  let component: FiltersDataComponent;
  let fixture: ComponentFixture<FiltersDataComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ FiltersDataComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FiltersDataComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
