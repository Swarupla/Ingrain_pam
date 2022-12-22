import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ArchivedModelListComponent } from './archived-model-list.component';

describe('ArchivedModelListComponent', () => {
  let component: ArchivedModelListComponent;
  let fixture: ComponentFixture<ArchivedModelListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ArchivedModelListComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ArchivedModelListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
